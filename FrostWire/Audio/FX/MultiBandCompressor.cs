using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Mix;

namespace FrostWire.Audio.FX
{
    public class MultiBandCompressor
    {
        private Mixer _mixer;
        private int _subBandStream;
        private int _lowBandStream;
        private int _midBandStream;
        private int _highBandStream;

        public MultiBandCompressor(Mixer mixer)
        {
            _mixer = mixer;
            _mixer.OutputHandle = CreateMixer();
            (_subBandStream, _lowBandStream, _midBandStream, _highBandStream) = CreateStreams(_mixer.InputHandle);
            SetEQs(_subBandStream, _lowBandStream, _midBandStream, _highBandStream);
            SetComps(_lowBandStream, _midBandStream, _highBandStream);

            BassMix.BASS_Mixer_StreamAddChannel(_mixer.OutputHandle, _subBandStream, BASSFlag.BASS_MIXER_CHAN_NORAMPIN);
            BassMix.BASS_Mixer_StreamAddChannel(_mixer.OutputHandle, _lowBandStream, BASSFlag.BASS_MIXER_CHAN_NORAMPIN);
            BassMix.BASS_Mixer_StreamAddChannel(_mixer.OutputHandle, _midBandStream, BASSFlag.BASS_MIXER_CHAN_NORAMPIN);
            BassMix.BASS_Mixer_StreamAddChannel(_mixer.OutputHandle, _highBandStream, BASSFlag.BASS_MIXER_CHAN_NORAMPIN);
        }
        private int CreateMixer()
        {
            return BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_MIXER_NONSTOP | BASSFlag.BASS_SAMPLE_FLOAT);
        }
        private (int sub, int low, int mid, int high) CreateStreams(int mixerHandle)
        {
            int subBandStream = BassMix.BASS_Split_StreamCreate(mixerHandle, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_DECODE, null);
            int lowBandStream = BassMix.BASS_Split_StreamCreate(mixerHandle, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_DECODE, null);
            int midBandStream = BassMix.BASS_Split_StreamCreate(mixerHandle, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_DECODE, null);
            int highBandStream = BassMix.BASS_Split_StreamCreate(mixerHandle, BASSFlag.BASS_DEFAULT | BASSFlag.BASS_STREAM_DECODE, null);
            return (subBandStream, lowBandStream, midBandStream, highBandStream);
        }
        private void SetEQs(int subBandStream, int lowBandStream, int midBandStream, int highBandStream)
        {
            float subLowCrossover = 120;
            float lowMidCrossover = 500;
            float midHighCrossover = 4000;

            // Для LR 4-го порядка используем два каскада ФНЧ/ФВЧ 2-го порядка с Q=0.707
            float qButterworth = 0.707f; // Q для Баттерворта 2-го порядка

            // САБ: ФНЧ 4-го порядка (два каскада)
            var subHandle1 = Bass.BASS_ChannelSetFX(subBandStream, BASSFXType.BASS_FX_BFX_BQF, 11);
            Bass.BASS_FXSetParameters(subHandle1, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_LOWPASS,
                fCenter = subLowCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            var subHandle2 = Bass.BASS_ChannelSetFX(subBandStream, BASSFXType.BASS_FX_BFX_BQF, 10);
            Bass.BASS_FXSetParameters(subHandle2, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_LOWPASS,
                fCenter = subLowCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // НЧ полоса: ФВЧ 4-го порядка 120 Гц + ФНЧ 4-го порядка 500 Гц
            // ФВЧ 120 Гц (первый каскад)
            var lowHandle1 = Bass.BASS_ChannelSetFX(lowBandStream, BASSFXType.BASS_FX_BFX_BQF, 11);
            Bass.BASS_FXSetParameters(lowHandle1, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = subLowCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФВЧ 120 Гц (второй каскад)
            var lowHandle2 = Bass.BASS_ChannelSetFX(lowBandStream, BASSFXType.BASS_FX_BFX_BQF, 10);
            Bass.BASS_FXSetParameters(lowHandle2, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = subLowCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФНЧ 500 Гц (первый каскад)
            var lowHandle3 = Bass.BASS_ChannelSetFX(lowBandStream, BASSFXType.BASS_FX_BFX_BQF, 13);
            Bass.BASS_FXSetParameters(lowHandle3, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_LOWPASS,
                fCenter = lowMidCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФНЧ 500 Гц (второй каскад)
            var lowHandle4 = Bass.BASS_ChannelSetFX(lowBandStream, BASSFXType.BASS_FX_BFX_BQF, 12);
            Bass.BASS_FXSetParameters(lowHandle4, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_LOWPASS,
                fCenter = lowMidCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // СЧ полоса: аналогично с частотами 500 Гц и 4000 Гц
            // ФВЧ 500 Гц (каскад 1)
            var midHandle1 = Bass.BASS_ChannelSetFX(midBandStream, BASSFXType.BASS_FX_BFX_BQF, 11);
            Bass.BASS_FXSetParameters(midHandle1, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = lowMidCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФВЧ 500 Гц (каскад 2)
            var midHandle2 = Bass.BASS_ChannelSetFX(midBandStream, BASSFXType.BASS_FX_BFX_BQF, 10);
            Bass.BASS_FXSetParameters(midHandle2, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = lowMidCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФНЧ 4000 Гц (каскад 1)
            var midHandle3 = Bass.BASS_ChannelSetFX(midBandStream, BASSFXType.BASS_FX_BFX_BQF, 13);
            Bass.BASS_FXSetParameters(midHandle3, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_LOWPASS,
                fCenter = midHighCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФНЧ 4000 Гц (каскад 2)
            var midHandle4 = Bass.BASS_ChannelSetFX(midBandStream, BASSFXType.BASS_FX_BFX_BQF, 12);
            Bass.BASS_FXSetParameters(midHandle4, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_LOWPASS,
                fCenter = midHighCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ВЧ полоса: ФВЧ 4000 Гц 4-го порядка
            var highHandle1 = Bass.BASS_ChannelSetFX(highBandStream, BASSFXType.BASS_FX_BFX_BQF, 11);
            Bass.BASS_FXSetParameters(highHandle1, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = midHighCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            var highHandle2 = Bass.BASS_ChannelSetFX(highBandStream, BASSFXType.BASS_FX_BFX_BQF, 10);
            Bass.BASS_FXSetParameters(highHandle2, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = midHighCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });
        }
        private void SetComps(int lowBandStream, int midBandStream, int highBandStream)
        {
            
        }
    }
}