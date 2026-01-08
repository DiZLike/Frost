using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Mix;

namespace gainer.Audio
{
    public class AudioReader
    {
        public event Action<double>? ProgressChanged;

        //private int _mainHandle = 0;
        private bool _disposed = false;
        private int _progressUpdateCounter = 0;
        private const int PROGRESS_UPDATE_INTERVAL = 10; // Обновляем каждые 10 блоков

        public int Stream {  get; private set; }
        public int MainStream { get; private set; }
        public int SubStream { get; private set; }
        public int LowStream { get; private set; }
        public int MidStream { get; private set; }
        public int HighStream { get; private set; }

        public AudioReader(string filePath)
        {
            // Создаем поток для декодирования
            Stream = Bass.BASS_StreamCreateFile(
                filePath,
                0, 0,
                BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT
            );

            if (Stream == 0)
                throw new Exception($"Не удалось открыть файл: {Bass.BASS_ErrorGetCode()}");

            (MainStream, SubStream, LowStream, MidStream, HighStream) = SplitStream(Stream);
            SetFilteres(SubStream, LowStream, MidStream, HighStream);
        }

        private (int main, int sub, int low, int mid, int high) SplitStream(int stream)
        {
            int h_main = BassMix.BASS_Split_StreamCreate(stream, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT, null);
            int h_sub = BassMix.BASS_Split_StreamCreate(stream, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT, null);
            int h_low = BassMix.BASS_Split_StreamCreate(stream, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT, null);
            int h_mid = BassMix.BASS_Split_StreamCreate(stream, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT, null);
            int h_high = BassMix.BASS_Split_StreamCreate(stream, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT, null);

            return (h_main, h_sub, h_low, h_mid, h_high);
        }
        private void SetFilteres(int sub, int low, int mid, int high)
        {
            BassFx.BASS_FX_GetVersion();
            float subLowCrossover = 120;
            float lowMidCrossover = 500;
            float midHighCrossover = 4000;

            // Для LR 4-го порядка используем два каскада ФНЧ/ФВЧ 2-го порядка с Q=0.707
            float qButterworth = 0.707f; // Q для Баттерворта 2-го порядка

            // САБ: ФНЧ 4-го порядка (два каскада)
            var subHandle1 = Bass.BASS_ChannelSetFX(sub, BASSFXType.BASS_FX_BFX_BQF, 11);
            var err = Bass.BASS_ErrorGetCode();
            Bass.BASS_FXSetParameters(subHandle1, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_LOWPASS,
                fCenter = subLowCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            

            var subHandle2 = Bass.BASS_ChannelSetFX(sub, BASSFXType.BASS_FX_BFX_BQF, 10);
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
            var lowHandle1 = Bass.BASS_ChannelSetFX(low, BASSFXType.BASS_FX_BFX_BQF, 11);
            Bass.BASS_FXSetParameters(lowHandle1, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = subLowCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФВЧ 120 Гц (второй каскад)
            var lowHandle2 = Bass.BASS_ChannelSetFX(low, BASSFXType.BASS_FX_BFX_BQF, 10);
            Bass.BASS_FXSetParameters(lowHandle2, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = subLowCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФНЧ 500 Гц (первый каскад)
            var lowHandle3 = Bass.BASS_ChannelSetFX(low, BASSFXType.BASS_FX_BFX_BQF, 13);
            Bass.BASS_FXSetParameters(lowHandle3, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_LOWPASS,
                fCenter = lowMidCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФНЧ 500 Гц (второй каскад)
            var lowHandle4 = Bass.BASS_ChannelSetFX(low, BASSFXType.BASS_FX_BFX_BQF, 12);
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
            var midHandle1 = Bass.BASS_ChannelSetFX(mid, BASSFXType.BASS_FX_BFX_BQF, 11);
            Bass.BASS_FXSetParameters(midHandle1, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = lowMidCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФВЧ 500 Гц (каскад 2)
            var midHandle2 = Bass.BASS_ChannelSetFX(mid, BASSFXType.BASS_FX_BFX_BQF, 10);
            Bass.BASS_FXSetParameters(midHandle2, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = lowMidCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФНЧ 4000 Гц (каскад 1)
            var midHandle3 = Bass.BASS_ChannelSetFX(mid, BASSFXType.BASS_FX_BFX_BQF, 13);
            Bass.BASS_FXSetParameters(midHandle3, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_LOWPASS,
                fCenter = midHighCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ФНЧ 4000 Гц (каскад 2)
            var midHandle4 = Bass.BASS_ChannelSetFX(mid, BASSFXType.BASS_FX_BFX_BQF, 12);
            Bass.BASS_FXSetParameters(midHandle4, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_LOWPASS,
                fCenter = midHighCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            // ВЧ полоса: ФВЧ 4000 Гц 4-го порядка
            var highHandle1 = Bass.BASS_ChannelSetFX(high, BASSFXType.BASS_FX_BFX_BQF, 11);
            Bass.BASS_FXSetParameters(highHandle1, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = midHighCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });

            var highHandle2 = Bass.BASS_ChannelSetFX(high, BASSFXType.BASS_FX_BFX_BQF, 10);
            Bass.BASS_FXSetParameters(highHandle2, new BASS_BFX_BQF()
            {
                lFilter = BASSBFXBQF.BASS_BFX_BQF_HIGHPASS,
                fCenter = midHighCrossover,
                fBandwidth = 0f,
                fQ = qButterworth,
                fGain = 0f
            });
        }

        public float[] ReadAllSamples(int stream)
        {
            Bass.BASS_ChannelSetPosition(stream, 0);
            if (_disposed)
                throw new ObjectDisposedException(nameof(AudioReader));

            List<float> samples = new List<float>();
            float[] buffer = new float[44100 * 2]; // 1 секунда стерео

            // Получаем общее количество сэмплов для расчета прогресса
            long totalBytes = Bass.BASS_ChannelGetLength(stream);
            long bytesProcessed = 0;

            int bytesRead;
            while ((bytesRead = Bass.BASS_ChannelGetData(stream, buffer, buffer.Length * 4)) > 0)
            {
                bytesProcessed += bytesRead;
                int samplesRead = bytesRead / 4;
                samples.AddRange(buffer.Take(samplesRead));

                // Оповещаем о прогрессе чтения (0%-50%)
                if (totalBytes > 0 && _progressUpdateCounter % PROGRESS_UPDATE_INTERVAL == 0)
                {
                    double progress = (bytesProcessed / (double)totalBytes) * 0.5; // Чтение - половина процесса
                    ProgressChanged?.Invoke(progress);
                }
                _progressUpdateCounter++;
            }
            var error = Bass.BASS_ErrorGetCode();
            return samples.ToArray();
        }

        public float[] GetPCMData32(int stream)
        {
            try
            {
                return ReadAllSamples(stream);
            }
            finally
            {
                
            }
        }
        public void Free()
        {
            Bass.BASS_ChannelFree(Stream);
            Bass.BASS_ChannelFree(MainStream);
            Bass.BASS_ChannelFree(SubStream);
            Bass.BASS_ChannelFree(LowStream);
            Bass.BASS_ChannelFree(MidStream);
            Bass.BASS_ChannelFree(HighStream);
        }
    }
}