﻿#region

using System;
using System.IO;
using System.Threading.Tasks;
using Hearthstone_Deck_Tracker.HsReplay.API;
using Hearthstone_Deck_Tracker.Utility.Logging;
using static Hearthstone_Deck_Tracker.HsReplay.Constants;

#endregion

namespace Hearthstone_Deck_Tracker.HsReplay
{
	internal class HsReplayManager
	{
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
	}
}