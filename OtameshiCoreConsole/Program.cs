using System;
using System.Threading.Tasks;


namespace HirosakiUniversity.Aldente.AES
{
	using Data.Standard;

	namespace OtameshiCoreConsole
	{
		class Program
		{
			static async Task Main(string[] args)
			{
				Console.WriteLine("Hello. Welcome to AES.");

				try
				{
					await Talk();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
				Console.WriteLine("Good bye. Good luck.");
			}

			static async Task Talk()
			{
				while (true)
				{
					var key = Console.ReadKey();
					switch (key.Key)
					{
						case ConsoleKey.L:
							await LoadData(@"D:\Users\aldente\OneDrive\OneDrive - Hirosaki University\storage\aes\namiki\20161208\data003.A\");
							break;
						case ConsoleKey.C:
							LoadCondition(@"B:\namiki_Zr.fcd");
							break;
						case ConsoleKey.F:
							await ExecuteFitting();
							break;
						case ConsoleKey.Q:
							return;

					}

				}


			}

			static async Task LoadData(string directory)
			{
				await data.LoadFromAsync(directory);
			}

			static void LoadCondition(string fileName)
			{
				data.LoadFittingCondition(fileName);
			}

			static async Task ExecuteFitting()
			{
				await data.FitAsync();
			}

			static DepthProfileFittingData data = new DepthProfileFittingData();


		}
	}
}