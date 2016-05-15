using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SylphyHorn.Interop;
using SylphyHorn.Serialization;
using WindowsDesktop;

namespace SylphyHorn.Services
{
	public class WallpaperService : IDisposable
	{
		private static readonly string[] _supportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", };

		public static WallpaperService Instance { get; } = new WallpaperService();

		private WallpaperService()
		{
			VirtualDesktop.CurrentChanged += VirtualDesktopOnCurrentChanged;
		}

		private static void VirtualDesktopOnCurrentChanged(object sender, VirtualDesktopChangedEventArgs e)
		{
			Task.Run(() => 
			//VisualHelper.InvokeOnUIDispatcher(() =>
			{
				if (!Settings.General.ChangeBackgroundEachDesktop) return;

				var desktops = VirtualDesktop.GetDesktops();
				var newIndex = Array.IndexOf(desktops, e.NewDesktop) + 1;

				var dirPath = Settings.General.DesktopBackgroundFolderPath.Value ?? "";
				if (dirPath.Contains(";"))
				{
					var dirPaths = dirPath.Split(';');
					dirPath = dirPaths[(newIndex - 1) % dirPaths.Length];
					SetSlideshow(dirPath);
				}
				else if (Directory.Exists(dirPath))
				{
					foreach (var extension in _supportedExtensions)
					{
						var filePath = Path.Combine(dirPath, newIndex + extension);
						var wallpaper = new FileInfo(filePath);
						if (wallpaper.Exists)
						{
							Set(wallpaper.FullName);
							break;
						}
					}
				}
			});
		}

		public void Dispose()
		{
			VirtualDesktop.CurrentChanged -= VirtualDesktopOnCurrentChanged;
		}

		public static void Set(string path)
		{
			NativeMethods.SystemParametersInfo(
				SystemParametersInfo.SPI_SETDESKWALLPAPER,
				0,
				path,
				SystemParametersInfoFlag.SPIF_UPDATEINIFILE | SystemParametersInfoFlag.SPIF_SENDWININICHANGE);
		}

		private static void SetSlideshow(string path)
		{
			var iniPath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\Microsoft\Windows\Themes\slideshow.ini");
			var ini =
$@"[Slideshow]
ImagesRootPath={path}
";
			var iniAttrs = File.GetAttributes(iniPath);
			File.SetAttributes(iniPath, FileAttributes.Normal);
			File.WriteAllText(iniPath, ini);
			File.SetAttributes(iniPath, iniAttrs);

			// TODO: refresh settings (restart explorer.exe is terrible...)
		}
	}
}
