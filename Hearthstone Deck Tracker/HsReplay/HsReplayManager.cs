﻿#region

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.HsReplay.API;
using Hearthstone_Deck_Tracker.HsReplay.Converter;
using Hearthstone_Deck_Tracker.Replay;
using Hearthstone_Deck_Tracker.Stats;
using Hearthstone_Deck_Tracker.Utility.Logging;
using Hearthstone_Deck_Tracker.Utility.Toasts;
using Hearthstone_Deck_Tracker.Utility.Toasts.ToastControls;

#endregion

namespace Hearthstone_Deck_Tracker.HsReplay
{
	internal class HsReplayManager
	{
		public static async Task<bool> ShowReplay(GameStats game, bool showToast)
		{
			if(game == null)
				return false;
			if(Config.Instance.ForceLocalReplayViewer)
			{
				ReplayReader.LaunchReplayViewer(game.ReplayFile);
				return true;
			}
			Action<ReplayProgress> setToastStatus = null;
			if(game.HasReplayFile && (!game.HsReplay?.Uploaded ?? true))
			{
				if(showToast)
					setToastStatus = ToastManager.ShowReplayProgressToast();
				var log = GetLogFromHdtReplay(game.ReplayFile);
				var validationResult = LogValidator.Validate(log);
				if(validationResult.Valid)
				{
					var result = await LogUploader.Upload(log.ToArray(), null, game);
					if(result.Success)
					{
						game.HsReplay = new HsReplayInfo(result.ReplayId);
						if(DefaultDeckStats.Instance.DeckStats.Any(x => x.DeckId == game.DeckId))
							DefaultDeckStats.Save();
						else
							DeckStatsList.Save();
					}
				}
			}
			if(game.HsReplay?.Uploaded ?? false)
			{
				setToastStatus?.Invoke(ReplayProgress.Complete);
				Helper.TryOpenUrl(game.HsReplay?.Url);
			}
			else if(game.HasReplayFile)
			{
				setToastStatus?.Invoke(ReplayProgress.Error);
				ReplayReader.LaunchReplayViewer(game.ReplayFile);
			}
			else
			{
				setToastStatus?.Invoke(ReplayProgress.Error);
				return false;
			}
			return true;
		}

		private static List<string> GetLogFromHdtReplay(string file)
		{
			var path = Path.Combine(Config.Instance.ReplayDir, file);
			if(!File.Exists(path))
				return new List<string>();
			try
			{
				using(var fs = new FileStream(path, FileMode.Open))
				using(var archive = new ZipArchive(fs, ZipArchiveMode.Read))
				using(var sr = new StreamReader(archive.GetEntry("output_log.txt").Open()))
					return sr.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
			}
			catch(Exception e)
			{
				Log.Error(e);
				return new List<string>();
			}
		}

		public static async Task<bool> Setup()
		{
			try
			{
				//await ApiManager.UpdateAccountStatus();
				return true;
			}
			catch(Exception e)
			{
				Log.Error(e);
				return false;
			}
		}

		public static async Task ShowReplay(string fileName, bool showToast)
		{
			if(Config.Instance.ForceLocalReplayViewer)
			{
				ReplayReader.LaunchReplayViewer(fileName);
				return;
			}
			Action<ReplayProgress> setToastStatus = null;
			var log = GetLogFromHdtReplay(fileName);
			var validationResult = LogValidator.Validate(log);
			if(validationResult.Valid)
			{

				if(showToast && setToastStatus == null)
					setToastStatus = ToastManager.ShowReplayProgressToast();
				setToastStatus?.Invoke(ReplayProgress.Uploading);
				var result = await LogUploader.Upload(log.ToArray(), null, null);
				if(result.Success)
					Helper.TryOpenUrl(new HsReplayInfo(result.ReplayId).Url);
				else
					ReplayReader.LaunchReplayViewer(fileName);
			}
			else
				ReplayReader.LaunchReplayViewer(fileName);
			setToastStatus?.Invoke(ReplayProgress.Complete);
		}
	}
}