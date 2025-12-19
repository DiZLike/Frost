using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gainer.cs
{
	public class KFilter
	{
		private readonly float[] _b;                                                        // Коэффициенты числителя (feedforward)
		private readonly float[] _a;                                                        // Коэффициенты знаменателя (feedback)
		private float[] _z;                                                                 // Состояние фильтра

		public KFilter()
		{
			// Коэффициенты для K-фильтра
			_b = new float[] { 1.53512485958697f, -2.69169618940638f, 1.19839281085285f };
			_a = new float[] { 1.0f, -1.69065929318241f, 0.73248077421585f };				
			_z = new float[_a.Length - 1];                                                  // Инициализация состояния фильтра
		}

		public float[] ApplyFilter(float[] input)
		{
			float[] output = new float[input.Length];
			for (int i = 0; i < input.Length; i++)
			{
				// Вычисление выходного значения
				output[i] = _b[0] * input[i] + _z[0];

				// Обновление состояния фильтра
				for (int j = 1; j < _a.Length; j++)
				{
					if (j < _z.Length)
					{
						_z[j - 1] = _b[j] * input[i] + _z[j] - _a[j] * output[i];
						if (i % 10000 == 0 || i == input.Length - 1)
							Console.Write($"\r\tApplication of the K-filter: \t{i + 1}");
					}
					else
					{
						_z[j - 1] = _b[j] * input[i] - _a[j] * output[i];
					}
				}
			}
			Console.WriteLine();
			Console.WriteLine();
			return output;
		}
	}

}
