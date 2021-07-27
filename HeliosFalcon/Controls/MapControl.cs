﻿//  Copyright 2014 Craig Courtney
//  Copyright 2021 Helios Contributors
//    
//  Helios is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  Helios is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace GadrocsWorkshop.Helios.Controls
{
	using GadrocsWorkshop.Helios.ComponentModel;
	using GadrocsWorkshop.Helios.Interfaces.Falcon;
	using System.Windows;
	using System.Windows.Media;
	using System.Xml;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Globalization;


	[HeliosControl("Helios.Falcon.MapControl", "Map Control", "Falcon Simulator", typeof(Gauges.GaugeRenderer))]

	public class MapControl : Gauges.BaseGauge
	{
		private FalconInterface _falconInterface;

		private HeliosValue _mapRotationEnable;
		private HeliosValue _mapScaleChange;
		private HeliosValue _bullseyeVisible;
		private HeliosValue _threatsVisible;
		private HeliosValue _waypointsVisible;

		private Gauges.GaugeImage _Background;
		private Gauges.GaugeImage _Foreground;
		private Gauges.GaugeImage _MapNoData;
		private Gauges.CustomGaugeNeedle _Map;
		private Gauges.CustomGaugeNeedle _Bullseye;
		private Gauges.CustomGaugeNeedle _RangeRings;
		private Gauges.CustomGaugeNeedle _Aircraft;
		private MapControlRenderer _MapOverlays;

		private Rect _imageSize = new Rect(0d, 0d, 200d, 200d);
		private Size _needleSize = new Size(200d, 200d);
		private Rect _needleClip = new Rect(1d, 1d, 198d, 198d);
		private Point _needleLocation = new Point(0d, 0d);
		private Point _needleCenter = new Point(100d, 100d);

		private const string _mapBullseyeImage64 = "{HeliosFalcon}/Images/MapControl/Map Bullseye 64.png";
		private const string _mapBullseyeImage128 = "{HeliosFalcon}/Images/MapControl/Map Bullseye 128.png";
		private const string _mapRangeRingsImage = "{HeliosFalcon}/Images/MapControl/Map Range Rings.png";
		private const string _mapAircraftImage = "{HeliosFalcon}/Images/MapControl/Map Aircraft.png";
		private const string _mapNoDataImage = "{HeliosFalcon}/Images/MapControl/Map No Data.png";
		private const string _backgroundImage = "{HeliosFalcon}/Images/MapControl/Background.png";
		private const string _foregroundImage = "{HeliosFalcon}/Images/MapControl/Foreground.png";

		private const double _mapBaseScale = 2.2d;
		private const double _mapSizeFeet64 = 3358700;   // 1024 km x 3279.98 ft/km (BMS conversion value)
		private const double _mapSizeFeet128 = 6717400;  // 2048 km x 3279.98 ft/km (BMS conversion value)

		private double _mapSizeFeet = 3358700;
		private double _mapScaleMultiplier = 1d;  // 1d = 60Nm, 2d = 30Nm, 4d = 15Nm
		private double _mapSizeMultiplier = 1d;   // 1d = 64 Segment, 2d = 128 Segment
		private double _mapModifiedScale = 0d;
		private double _mapRotationAngle = 0d;
		private double _mapVerticalValue = 0d;
		private double _mapHorizontalValue = 0d;
		private double _bullseyeVerticalValue = 0d;
		private double _bullseyeHorizontalValue = 0d;
		private double _rangeScale = 0d;
		private int _rangeInitialHorizontal = 0;
		private int _mapInitialHorizontal = 0;
		private int _rangeInitialVertical = 0;
		private int _mapInitialVertical = 0;
		private bool _mapRotation_Enabled = false;
		private bool _mapImageChanged = false;
		private bool _profileFirstStart = true;
		private bool _inFlight = false;
		private bool _mapSelectionCompleted = false;
		private bool _navPointsInitialized = false;
		private bool _refreshPending = false;
		private string _lastTheater;

		private string[,] _mapBaseImages = new string[,]
		{	{ "101", "{HeliosFalcon}/Images/Maps/Aegean/Map.jpg", "1", "Aegean" },
			{ "102", "{HeliosFalcon}/Images/Maps/Balkans/Map.jpg", "1", "Balkans, BFB 1.2.1" },
			{ "103", "{HeliosFalcon}/Images/Maps/CentralEurope/Map.jpg", "1", "CentralEurope, Central Europe, CET" },
			{ "104", "{HeliosFalcon}/Images/Maps/EMF/Map.jpg", "2", "EMF, EMFL 35.0.6a" },
			{ "105", "{HeliosFalcon}/Images/Maps/Iberia/Map.jpg", "2", "Iberia, POH" },
			{ "106", "{HeliosFalcon}/Images/Maps/Ikaros/Map.jpg", "1", "Ikaros" },
			{ "107", "{HeliosFalcon}/Images/Maps/Israel/Map.jpg", "1", "Israel, bfs2.2.4sp" },
			{ "108", "{HeliosFalcon}/Images/Maps/Korea/Map.jpg", "1", "Korea KTO, Korea TvT, RedFlag, Red Flag" },
			{ "109", "{HeliosFalcon}/Images/Maps/Kuwait/Map.jpg", "1", "Kuwait" },
			{ "110", "{HeliosFalcon}/Images/Maps/Libya/Map.jpg", "2", "Libya" },
			{ "111", "{HeliosFalcon}/Images/Maps/Nevada/Map.jpg", "1", "Nevada" },
			{ "112", "{HeliosFalcon}/Images/Maps/Nordic/Map.jpg", "1", "Nordic, NTO, NTO Polar Vortex, CWC, NordicCWC" },
			{ "113", "{HeliosFalcon}/Images/Maps/Panama/Map.jpg", "1", "Panama" },
			{ "114", "{HeliosFalcon}/Images/Maps/Taiwan/Map.jpg", "1", "Taiwan" },
			{ "115", "", "1", "" },
			{ "116", "", "1", "" },
			{ "117", "", "1", "" },
			{ "118", "", "1", "" },
			{ "119", "", "1", "" },
			{ "120", "", "1", "" },
			{ "121", "", "1", "" },
			{ "122", "", "1", "" },
			{ "123", "", "1", "" },
			{ "124", "", "1", "" } };

		private string[,] _mapUserImages = new string[,]
		{	{ "201", "{HeliosFalcon}/Images/Maps/Korea/Map.jpg", "1", "Korea KTO" },
			{ "202", "{HeliosFalcon}/Images/Maps/Korea/Map.jpg", "1", "Korea KTO" },
			{ "203", "{HeliosFalcon}/Images/Maps/Korea/Map.jpg", "1", "Korea KTO" },
			{ "204", "{HeliosFalcon}/Images/Maps/Korea/Map.jpg", "1", "Korea KTO" },
			{ "205", "{HeliosFalcon}/Images/Maps/Korea/Map.jpg", "1", "Korea KTO" },
			{ "206", "{HeliosFalcon}/Images/Maps/Korea/Map.jpg", "1", "Korea KTO" },
			{ "207", "{HeliosFalcon}/Images/Maps/Korea/Map.jpg", "1", "Korea KTO" },
			{ "208", "{HeliosFalcon}/Images/Maps/Korea/Map.jpg", "1", "Korea KTO" }, };


		public MapControl()
			: base("MapControl", new Size(200d, 200d))
		{
			_Background = new Gauges.GaugeImage(_backgroundImage, _imageSize);
			Components.Add(_Background);

			_Map = new Gauges.CustomGaugeNeedle(_mapBaseImages[7, 1], _needleLocation, _needleSize, _needleCenter);
			_Map.Clip = new RectangleGeometry(_needleClip);
			_Map.ImageRefresh = true;
			Components.Add(_Map);

			_Bullseye = new Gauges.CustomGaugeNeedle(_mapBullseyeImage64, _needleLocation, _needleSize, _needleCenter);
			_Bullseye.Clip = new RectangleGeometry(_needleClip);
			_Bullseye.IsHidden = true;
			Components.Add(_Bullseye);

			_MapOverlays = new MapControlRenderer(_needleLocation, _needleSize, _needleCenter);
			_MapOverlays.Clip = new RectangleGeometry(_needleClip);
			Components.Add(_MapOverlays);
			
			_RangeRings = new Gauges.CustomGaugeNeedle(_mapRangeRingsImage, _needleLocation, _needleSize, _needleCenter);
			_RangeRings.Clip = new RectangleGeometry(_needleClip);
			Components.Add(_RangeRings);

			_Aircraft = new Gauges.CustomGaugeNeedle(_mapAircraftImage, _needleLocation, _needleSize, _needleCenter);
			_Aircraft.Clip = new RectangleGeometry(_needleClip);
			Components.Add(_Aircraft);

			_Foreground = new Gauges.GaugeImage(_foregroundImage, _imageSize);
			_Foreground.IsHidden = true;
			Components.Add(_Foreground);

			_MapNoData = new Gauges.GaugeImage(_mapNoDataImage, _imageSize);
			_MapNoData.IsHidden = true;
			Components.Add(_MapNoData);

			_mapRotationEnable = new HeliosValue(this, new BindingValue(false), "", "Map North Up vs Heading Up", "Sets North Up or Heading Up map orientation.", "Set true for Heading Up orientation.", BindingValueUnits.Boolean);
			_mapRotationEnable.Execute += new HeliosActionHandler(MapRotationEnable_Execute);
			Actions.Add(_mapRotationEnable);
			Values.Add(_mapRotationEnable);

			_mapScaleChange = new HeliosValue(this, new BindingValue(0d), "", "Map Scale", "Sets the scale of the map.", "1 = 60Nm, 2 = 30Nm, 3 = 15Nm, Default = 2", BindingValueUnits.Numeric);
			_mapScaleChange.Execute += new HeliosActionHandler(MapScaleChange_Execute);
			Actions.Add(_mapScaleChange);
			Values.Add(_mapScaleChange);

			_bullseyeVisible = new HeliosValue(this, new BindingValue(false), "", "Bullseye Visible", "Sets visibility of the bullseye.", "Set true to show bullseye.", BindingValueUnits.Boolean);
			_bullseyeVisible.Execute += new HeliosActionHandler(BullseyeVisible_Execute);
			Actions.Add(_bullseyeVisible);
			Values.Add(_bullseyeVisible);

			_threatsVisible = new HeliosValue(this, new BindingValue(false), "", "Pre-Planned Threats Visible", "Sets visibility of the pre-planned threats.", "Set true to show pre-planned threats.", BindingValueUnits.Boolean);
			_threatsVisible.Execute += new HeliosActionHandler(ThreatsVisible_Execute);
			Actions.Add(_threatsVisible);
			Values.Add(_threatsVisible);

			_waypointsVisible = new HeliosValue(this, new BindingValue(false), "", "Waypoints Visible", "Sets visibility of the waypoints.", "Set true to show waypoints.", BindingValueUnits.Boolean);
			_waypointsVisible.Execute += new HeliosActionHandler(WaypointsVisible_Execute);
			Actions.Add(_waypointsVisible);
			Values.Add(_waypointsVisible);

			MapControlStaticResize();
			MapControlDynamicResize(true);
			Resized += new EventHandler(OnMapControl_Resized);
		}


		#region Actions

		protected override void OnProfileChanged(HeliosProfile oldProfile)
		{
			base.OnProfileChanged(oldProfile);

			if (oldProfile != null)
			{
				oldProfile.ProfileStarted -= new EventHandler(Profile_ProfileStarted);
				oldProfile.ProfileTick -= new EventHandler(Profile_ProfileTick);
				oldProfile.ProfileStopped -= new EventHandler(Profile_ProfileStopped);
			}

			if (Profile != null)
			{
				Profile.ProfileStarted += new EventHandler(Profile_ProfileStarted);
				Profile.ProfileTick += new EventHandler(Profile_ProfileTick);
				Profile.ProfileStopped += new EventHandler(Profile_ProfileStopped);
			}
		}

		void Profile_ProfileStarted(object sender, EventArgs e)
		{
			if (Parent.Profile.Interfaces.ContainsKey("Falcon"))
			{
				_falconInterface = Parent.Profile.Interfaces["Falcon"] as FalconInterface;
			}

			if (_profileFirstStart)
			{
				_profileFirstStart = false;
				ShowNoDataPanel();
				MapScaleChange(2d);
			}
		}

		void Profile_ProfileTick(object sender, EventArgs e)
		{
			if (_falconInterface != null)
			{
				BindingValue Flying = GetValue("Runtime", "Flying");
				_inFlight = Flying.BoolValue;

				if (_inFlight && !_navPointsInitialized)
				{
					if (!_mapSelectionCompleted)
					{
						string theater = _falconInterface.GetCurrentTheater();

						if (theater != _lastTheater)
						{
							_lastTheater = theater;
							TheaterMapSelect(theater);
						}

						_mapSelectionCompleted = true;
					}

					List<string> navPoints = _falconInterface.NavPoints;

					if (navPoints != null && navPoints.Any())
					{
						_MapOverlays.ProcessNavPointValues(navPoints);
						_navPointsInitialized = true;
						Refresh();
					}

					HideNoDataPanel();
				}

				ProcessOwnshipValues();

				if (_refreshPending)
				{
					Refresh();
					_refreshPending = false;
				}

				if (!_inFlight)
				{
					_navPointsInitialized = false;
					_mapSelectionCompleted = false;
					ShowNoDataPanel();
				}
			}
		}

		void Profile_ProfileStopped(object sender, EventArgs e)
		{
			_falconInterface = null;
		}

		void ProcessOwnshipValues()
		{
			BindingValue mapRotationAngle = GetValue("HSI", "current heading");
			MapRotationAngle = mapRotationAngle.DoubleValue;

			BindingValue mapVerticalValue = GetValue("Ownship", "x");
			MapVerticalValue = mapVerticalValue.DoubleValue;

			BindingValue mapHorizontalValue = GetValue("Ownship", "y");
			MapHorizontalValue = mapHorizontalValue.DoubleValue;

			BindingValue bullseyeVerticalValue = GetValue("Ownship", "deltaX from bulls");
			BullseyeVerticalValue = bullseyeVerticalValue.DoubleValue;

			BindingValue bullseyeHorizontalValue = GetValue("Ownship", "deltaY from bulls");
			BullseyeHorizontalValue = bullseyeHorizontalValue.DoubleValue;

			if (_mapImageChanged)
			{
				_mapImageChanged = false;
				CalculateOffsets();
			}
		}

		private BindingValue GetValue(string device, string name)
		{
			return _falconInterface?.GetValue(device, name) ?? BindingValue.Empty;
		}

		public override void Reset()
		{
			BeginTriggerBypass(true);

			_Bullseye.IsHidden = true;
			_MapOverlays.ThreatVisibility = 0d;
			_MapOverlays.WaypointVisibility = 0d;
			_mapRotation_Enabled = false;
			MapRotationAngle = 0d;
			MapScaleChange(2d);

			EndTriggerBypass(true);
		}

		void ShowNoDataPanel()
		{
			_MapNoData.IsHidden = false;
			_Foreground.IsHidden = false;
		}

		void HideNoDataPanel()
		{
			_MapNoData.IsHidden = true;
			_Foreground.IsHidden = true;
		}

		void BullseyeVisible_Execute(object action, HeliosActionEventArgs e)
		{
			_bullseyeVisible.SetValue(e.Value, e.BypassCascadingTriggers);
			_Bullseye.IsHidden = !_bullseyeVisible.Value.BoolValue;
		}

		void ThreatsVisible_Execute(object action, HeliosActionEventArgs e)
		{
			_threatsVisible.SetValue(e.Value, e.BypassCascadingTriggers);
			bool value = _threatsVisible.Value.BoolValue;

			_MapOverlays.ThreatVisibility = value ? 1.0d : 0d;
			_refreshPending = true;
		}

		void WaypointsVisible_Execute(object action, HeliosActionEventArgs e)
		{
			_waypointsVisible.SetValue(e.Value, e.BypassCascadingTriggers);
			bool value = _waypointsVisible.Value.BoolValue;

			_MapOverlays.WaypointVisibility = value ? 1.0d : 0d;
			_refreshPending = true;
		}

		#endregion Actions


		#region Map Selection

		void TheaterMapSelect(string theater)
		{
			double mapNumber = 0d;

			mapNumber = GetTheaterMapNumber(_mapBaseImages, theater);

			if (mapNumber == 0d)
			{
				mapNumber = GetTheaterMapNumber(_mapUserImages, theater);
			}

			if (mapNumber > 0d)
			{
				MapImageSelect(mapNumber);
			}
		}

		double GetTheaterMapNumber(string[,] mapImages, string theater)
		{
			double mapNumber = 0d;

			for (int i = 0; i < mapImages.GetLength(0); i++)
			{
				List<string> theaters = mapImages[i, 3].Split(',').Select(p => p.Trim()).ToList<string>();
				if (theaters.Contains(theater, StringComparer.OrdinalIgnoreCase))
				{
					mapNumber = Convert.ToDouble(mapImages[i, 0]);
				}
			}

			return mapNumber;
		}

		void MapImageSelect(double mapNumber)
		{
			if (mapNumber > 100d && mapNumber < 200d)
			{
				MapImageAssign(_mapBaseImages, mapNumber);

			}
			else if (mapNumber > 200d && mapNumber < 300d)
			{
				MapImageAssign(_mapUserImages, mapNumber);
			}

			if (_mapImageChanged)
			{
				if (_mapSizeMultiplier == 1d)
				{
					_mapSizeFeet = _mapSizeFeet64;
					_Bullseye.Image = _mapBullseyeImage64;
				}
				else if (_mapSizeMultiplier == 2d)
				{
					_mapSizeFeet = _mapSizeFeet128;
					_Bullseye.Image = _mapBullseyeImage128;
				}
				else
				{
					_mapSizeFeet = _mapSizeFeet64;
					_Bullseye.Image = _mapBullseyeImage64;
				}

				MapControlDynamicResize(true);
				CalculateOffsets();
			}

			void MapImageAssign(string[,] mapImages, double mapNum)
			{
				for (int i = 0; i < mapImages.GetLength(0); i++)
				{
					if (mapNumber == Convert.ToDouble(mapImages[i, 0]))
					{
						if (_Map.Image != mapImages[i, 1])
						{
							_Map.Image = mapImages[i, 1];
							_mapSizeMultiplier = Convert.ToDouble(mapImages[i, 2]);
							_mapImageChanged = true;
						}
					}
				}
			}
		}

		#endregion


		#region Map Scaling

		void OnMapControl_Resized(object sender, EventArgs e)
		{
			MapControlStaticResize();
			MapControlDynamicResize(true);
		}

		void MapControlStaticResize()
		{
			double _mapShortestSize = 0d;
			double rangeWidth = 0d;
			double rangeHeight = 0d;
			double rangeInitialHorizontal = 0d;
			double rangeInitialVertical = 0d;

			if (Height >= Width)
			{
				_mapShortestSize = Width;
				_rangeScale = Width / Height * _mapBaseScale;
				rangeWidth = 200d * _mapBaseScale;
				rangeHeight = 200d * _rangeScale;

				rangeInitialHorizontal = (200d - rangeWidth) / 2d;
				rangeInitialVertical = (200d - rangeHeight) / 2d;

				_MapNoData.Height = Width / Height * 200;
				_MapNoData.PosY = (200d - Width / Height * 200d) / 2d;
				_MapNoData.Width = 200d;
				_MapNoData.PosX = 0d;
			}
			else
			{
				_mapShortestSize = Height;
				_rangeScale = Height / Width * _mapBaseScale;
				rangeWidth = 200d * _rangeScale;
				rangeHeight = 200d * _mapBaseScale;

				rangeInitialHorizontal = (200d - rangeWidth) / 2d;
				rangeInitialVertical = (200d - rangeHeight) / 2d;

				_MapNoData.Height = 200d;
				_MapNoData.PosY = 0d;
				_MapNoData.Width = Height / Width * 200;
				_MapNoData.PosX = (200d - Height / Width * 200d) / 2d;
			}

			_MapOverlays.MapShortestSize = _mapShortestSize;

			_RangeRings.Tape_Width = rangeWidth;
			_Aircraft.Tape_Width = rangeWidth;
			_rangeInitialHorizontal = Convert.ToInt32(rangeInitialHorizontal);

			_RangeRings.Tape_Height = rangeHeight;
			_Aircraft.Tape_Height = rangeHeight;
			_rangeInitialVertical = Convert.ToInt32(rangeInitialVertical);
		}

		void MapControlDynamicResize(bool mapResized)
		{
			double mapScale = 0d;
			double mapWidth = 0d;
			double mapHeight = 0d;
			double mapInitialHorizontal = 0d;
			double mapInitialVertical = 0d;

			if (Height >= Width)
			{
				mapScale = _rangeScale * _mapScaleMultiplier * _mapSizeMultiplier;

				mapWidth = Height / Width * 200d * mapScale;
				mapInitialHorizontal = (200d - mapWidth) / 2d;
				mapHeight = 200d * mapScale;
				mapInitialVertical = (200d - mapHeight) / 2d;
			}
			else
			{
				mapScale = _rangeScale * _mapScaleMultiplier * _mapSizeMultiplier;

				mapHeight = Width / Height * 200d * mapScale;
				mapInitialVertical = (200d - mapHeight) / 2d;
				mapWidth = 200d * mapScale;
				mapInitialHorizontal = (200d - mapWidth) / 2d;
			}

			_mapModifiedScale = mapScale;

			_MapOverlays.MapWidth = mapWidth;
			_MapOverlays.MapHeight = mapHeight;
			_MapOverlays.MapSizeFeet = _mapSizeFeet;
			_MapOverlays.MapScaleMultiplier = _mapScaleMultiplier;

			_Map.Tape_Width = mapWidth;
			_MapOverlays.Tape_Width = mapWidth;
			_Bullseye.Tape_Width = mapWidth;
			_mapInitialHorizontal = Convert.ToInt32(mapInitialHorizontal);

			_Map.Tape_Height = mapHeight;
			_MapOverlays.Tape_Height = mapHeight;
			_Bullseye.Tape_Height = mapHeight;
			_mapInitialVertical = Convert.ToInt32(mapInitialVertical);

			_RangeRings.HorizontalOffset = _rangeInitialHorizontal;
			_RangeRings.VerticalOffset = _rangeInitialVertical;
			_Aircraft.HorizontalOffset = _rangeInitialHorizontal;
			_Aircraft.VerticalOffset = _rangeInitialVertical;

			Refresh();

			if (mapResized)
			{
				_Map.HorizontalOffset = _mapInitialHorizontal;
				_Map.VerticalOffset = _mapInitialVertical;
				_MapOverlays.HorizontalOffset = _mapInitialHorizontal;
				_MapOverlays.VerticalOffset = _mapInitialVertical;
				_Bullseye.HorizontalOffset = _mapInitialHorizontal;
				_Bullseye.VerticalOffset = _mapInitialVertical;
			}
		}

		void MapVerticalOffset_Calculate(double vValue)
		{
			double mapVerticalValue = vValue - _mapSizeFeet / 2;

			if (Height >= Width)
			{
				_Map.VerticalOffset = _mapInitialVertical + (mapVerticalValue / _mapSizeFeet * _mapModifiedScale * 200);
				_MapOverlays.VerticalOffset = _Map.VerticalOffset;
			}
			else
			{
				_Map.VerticalOffset = _mapInitialVertical + (mapVerticalValue / _mapSizeFeet * _mapBaseScale * _mapScaleMultiplier * _mapSizeMultiplier * 200);
				_MapOverlays.VerticalOffset = _Map.VerticalOffset;
			}
		}

		void MapHorizontalOffset_Calculate(double hValue)
		{
			double mapHorizontalValue = hValue - _mapSizeFeet / 2;

			if (Height >= Width)
			{
				_Map.HorizontalOffset = _mapInitialHorizontal - (mapHorizontalValue / _mapSizeFeet * _mapBaseScale * _mapScaleMultiplier * _mapSizeMultiplier * 200);
				_MapOverlays.HorizontalOffset = _Map.HorizontalOffset;
			}
			else
			{
				_Map.HorizontalOffset = _mapInitialHorizontal - (mapHorizontalValue / _mapSizeFeet * _mapModifiedScale * 200);
				_MapOverlays.HorizontalOffset = _Map.HorizontalOffset;
			}
		}

		void BullseyeVerticalOffset_Calculate(double bullseyeVerticalValue)
		{
			if (Height >= Width)
			{
				_Bullseye.VerticalOffset = _mapInitialVertical + (bullseyeVerticalValue / _mapSizeFeet * _mapModifiedScale * 200);
			}
			else
			{
				_Bullseye.VerticalOffset = _mapInitialVertical + (bullseyeVerticalValue / _mapSizeFeet * _mapBaseScale * _mapScaleMultiplier * _mapSizeMultiplier * 200);
			}
		}

		void BullseyeHorizontalOffset_Calculate(double bullseyeHorizontalValue)
		{
			if (Height >= Width)
			{
				_Bullseye.HorizontalOffset = _mapInitialHorizontal - (bullseyeHorizontalValue / _mapSizeFeet * _mapBaseScale * _mapScaleMultiplier * _mapSizeMultiplier * 200);
			}
			else
			{
				_Bullseye.HorizontalOffset = _mapInitialHorizontal - (bullseyeHorizontalValue / _mapSizeFeet * _mapModifiedScale * 200);
			}
		}

		void MapRotationEnable_Execute(object action, HeliosActionEventArgs e)
		{
			_mapRotationEnable.SetValue(e.Value, e.BypassCascadingTriggers);
			_mapRotation_Enabled = _mapRotationEnable.Value.BoolValue;
			MapRotationAngle_Calculate(MapRotationAngle);
		}

		void MapRotationAngle_Calculate(double angle)
		{
			if (_mapRotation_Enabled)
			{
				_Map.Rotation = -angle;
				_MapOverlays.Rotation = -angle;
				_Bullseye.Rotation = -angle;
				_RangeRings.Rotation = -angle;
				_Aircraft.Rotation = 0d;
			}
			else
			{
				_Map.Rotation = 0d;
				_MapOverlays.Rotation = 0d;
				_Bullseye.Rotation = 0d;
				_RangeRings.Rotation = 0d;
				_Aircraft.Rotation = angle;
			}
		}

		void MapScaleChange_Execute(object action, HeliosActionEventArgs e)
		{
			_mapScaleChange.SetValue(e.Value, e.BypassCascadingTriggers);
			double value = _mapScaleChange.Value.DoubleValue;
			MapScaleChange(value);
		}

		void MapScaleChange(double value)
		{ 
			if (value == 1d)
			{
				_mapScaleMultiplier = 1d;
			}
			else if (value == 2d)
			{
				_mapScaleMultiplier = 2d;
			}
			else if (value == 3d)
			{
				_mapScaleMultiplier = 4d;
			}
			else
			{
				_mapScaleMultiplier = 2d;
			}

			MapControlDynamicResize(false);
			CalculateOffsets();
		}

		void CalculateOffsets()
		{
			MapRotationAngle_Calculate(MapRotationAngle);
			MapVerticalOffset_Calculate(MapVerticalValue);
			MapHorizontalOffset_Calculate(MapHorizontalValue);
			BullseyeVerticalOffset_Calculate(BullseyeVerticalValue);
			BullseyeHorizontalOffset_Calculate(BullseyeHorizontalValue);
		}

		#endregion


		#region Properties

		private double MapRotationAngle
		{
			get
			{
				return _mapRotationAngle;
			}
			set
			{
				if ((_mapRotationAngle == 0d && value != 0)
					|| (_mapRotationAngle != 0d && !_mapRotationAngle.Equals(value)))
				{
					double oldValue = _mapRotationAngle;
					_mapRotationAngle = value;
					OnPropertyChanged("MapRotationAngle", oldValue, value, true);
					{
						MapRotationAngle_Calculate(_mapRotationAngle);
						_refreshPending = false;
					}
				}
			}
		}

		private double MapVerticalValue
		{
			get
			{
				return _mapVerticalValue;
			}
			set
			{
				if ((_mapVerticalValue == 0d && value != 0)
					|| (_mapVerticalValue != 0d && !_mapVerticalValue.Equals(value)))
				{
					double oldValue = _mapVerticalValue;
					_mapVerticalValue = value;
					OnPropertyChanged("MapVerticalValue", oldValue, value, true);
					{
						MapVerticalOffset_Calculate(_mapVerticalValue);
						_refreshPending = false;
					}
				}
			}
		}

		private double MapHorizontalValue
		{
			get
			{
				return _mapHorizontalValue;
			}
			set
			{
				if ((_mapHorizontalValue == 0d && value != 0)
					|| (_mapHorizontalValue != 0d && !_mapHorizontalValue.Equals(value)))
				{
					double oldValue = _mapHorizontalValue;
					_mapHorizontalValue = value;
					OnPropertyChanged("MapHorizontalValue", oldValue, value, true);
					{
						MapHorizontalOffset_Calculate(_mapHorizontalValue);
						_refreshPending = false;
					}
				}
			}
		}

		private double BullseyeVerticalValue
		{
			get
			{
				return _bullseyeVerticalValue;
			}
			set
			{
				if ((_bullseyeVerticalValue == 0d && value != 0)
					|| (_bullseyeVerticalValue != 0d && !_bullseyeVerticalValue.Equals(value)))
				{
					double oldValue = _bullseyeVerticalValue;
					_bullseyeVerticalValue = value;
					OnPropertyChanged("BullseyeVerticalValue", oldValue, value, true);
					{
						BullseyeVerticalOffset_Calculate(_bullseyeVerticalValue);
						_refreshPending = false;
					}
				}
			}
		}

		private double BullseyeHorizontalValue
		{
			get
			{
				return _bullseyeHorizontalValue;
			}
			set
			{
				if ((_bullseyeHorizontalValue == 0d && value != 0)
					|| (_bullseyeHorizontalValue != 0d && !_bullseyeHorizontalValue.Equals(value)))
				{
					double oldValue = _bullseyeHorizontalValue;
					_bullseyeHorizontalValue = value;
					OnPropertyChanged("BullseyeHorizontalValue", oldValue, value, true);
					{
						BullseyeHorizontalOffset_Calculate(_bullseyeHorizontalValue);
						_refreshPending = false;
					}
				}
			}
		}

		public string UserMapImage_201
		{
			get
			{
				return _mapUserImages[0, 1];
			}
			set
			{
				if ((_mapUserImages[0, 1] == null && value != null)
					|| (_mapUserImages[0, 1] != null && !_mapUserImages[0, 1].Equals(value)))
				{
					_mapUserImages[0, 1] = value;
				}
			}
		}

		public string UserMapName_201
		{
			get
			{
				return _mapUserImages[0, 3];
			}
			set
			{
				if ((_mapUserImages[0, 3] == null && value != null)
					|| (_mapUserImages[0, 3] != null && !_mapUserImages[0, 3].Equals(value)))
				{
					_mapUserImages[0, 3] = value;
				}
			}
		}

		public bool UserMapSize_201
		{
			get
			{
				return Convert.ToBoolean(_mapUserImages[0, 2] == "2" ? "True" : "False");
			}
			set
			{
				string newValue = (value ? "2" : "1");

				if (_mapUserImages[0, 2] != newValue)
				{
					_mapUserImages[0, 2] = newValue;
				}
			}
		}

		public string UserMapImage_202
		{
			get
			{
				return _mapUserImages[1, 1];
			}
			set
			{
				if ((_mapUserImages[1, 1] == null && value != null)
					|| (_mapUserImages[1, 1] != null && !_mapUserImages[1, 1].Equals(value)))
				{
					_mapUserImages[1, 1] = value;
				}
			}
		}

		public string UserMapName_202
		{
			get
			{
				return _mapUserImages[1, 3];
			}
			set
			{
				if ((_mapUserImages[1, 3] == null && value != null)
					|| (_mapUserImages[1, 3] != null && !_mapUserImages[1, 3].Equals(value)))
				{
					_mapUserImages[1, 3] = value;
				}
			}
		}

		public bool UserMapSize_202
		{
			get
			{
				return Convert.ToBoolean(_mapUserImages[1, 2] == "2" ? "True" : "False");
			}
			set
			{
				string newValue = (value ? "2" : "1");

				if (_mapUserImages[1, 2] != newValue)
				{
					_mapUserImages[1, 2] = newValue;
				}
			}
		}

		public string UserMapImage_203
		{
			get
			{
				return _mapUserImages[2, 1];
			}
			set
			{
				if ((_mapUserImages[2, 1] == null && value != null)
					|| (_mapUserImages[2, 1] != null && !_mapUserImages[2, 1].Equals(value)))
				{
					_mapUserImages[2, 1] = value;
				}
			}
		}

		public string UserMapName_203
		{
			get
			{
				return _mapUserImages[2, 3];
			}
			set
			{
				if ((_mapUserImages[2, 3] == null && value != null)
					|| (_mapUserImages[2, 3] != null && !_mapUserImages[2, 3].Equals(value)))
				{
					_mapUserImages[2, 3] = value;
				}
			}
		}

		public bool UserMapSize_203
		{
			get
			{
				return Convert.ToBoolean(_mapUserImages[2, 2] == "2" ? "True" : "False");
			}
			set
			{
				string newValue = (value ? "2" : "1");

				if (_mapUserImages[2, 2] != newValue)
				{
					_mapUserImages[2, 2] = newValue;
				}
			}
		}

		public string UserMapImage_204
		{
			get
			{
				return _mapUserImages[3, 1];
			}
			set
			{
				if ((_mapUserImages[3, 1] == null && value != null)
					|| (_mapUserImages[3, 1] != null && !_mapUserImages[3, 1].Equals(value)))
				{
					_mapUserImages[3, 1] = value;
				}
			}
		}

		public string UserMapName_204
		{
			get
			{
				return _mapUserImages[3, 3];
			}
			set
			{
				if ((_mapUserImages[3, 3] == null && value != null)
					|| (_mapUserImages[3, 3] != null && !_mapUserImages[3, 3].Equals(value)))
				{
					_mapUserImages[3, 3] = value;
				}
			}
		}

		public bool UserMapSize_204
		{
			get
			{
				return Convert.ToBoolean(_mapUserImages[3, 2] == "2" ? "True" : "False");
			}
			set
			{
				string newValue = (value ? "2" : "1");

				if (_mapUserImages[3, 2] != newValue)
				{
					_mapUserImages[3, 2] = newValue;
				}
			}
		}

		public string UserMapImage_205
		{
			get
			{
				return _mapUserImages[4, 1];
			}
			set
			{
				if ((_mapUserImages[4, 1] == null && value != null)
					|| (_mapUserImages[4, 1] != null && !_mapUserImages[4, 1].Equals(value)))
				{
					_mapUserImages[4, 1] = value;
				}
			}
		}

		public string UserMapName_205
		{
			get
			{
				return _mapUserImages[4, 3];
			}
			set
			{
				if ((_mapUserImages[4, 3] == null && value != null)
					|| (_mapUserImages[4, 3] != null && !_mapUserImages[4, 3].Equals(value)))
				{
					_mapUserImages[4, 3] = value;
				}
			}
		}

		public bool UserMapSize_205
		{
			get
			{
				return Convert.ToBoolean(_mapUserImages[4, 2] == "2" ? "True" : "False");
			}
			set
			{
				string newValue = (value ? "2" : "1");

				if (_mapUserImages[4, 2] != newValue)
				{
					_mapUserImages[4, 2] = newValue;
				}
			}
		}

		public string UserMapImage_206
		{
			get
			{
				return _mapUserImages[5, 1];
			}
			set
			{
				if ((_mapUserImages[5, 1] == null && value != null)
					|| (_mapUserImages[5, 1] != null && !_mapUserImages[5, 1].Equals(value)))
				{
					_mapUserImages[5, 1] = value;
				}
			}
		}

		public string UserMapName_206
		{
			get
			{
				return _mapUserImages[5, 3];
			}
			set
			{
				if ((_mapUserImages[5, 3] == null && value != null)
					|| (_mapUserImages[5, 3] != null && !_mapUserImages[5, 3].Equals(value)))
				{
					_mapUserImages[5, 3] = value;
				}
			}
		}

		public bool UserMapSize_206
		{
			get
			{
				return Convert.ToBoolean(_mapUserImages[5, 2] == "2" ? "True" : "False");
			}
			set
			{
				string newValue = (value ? "2" : "1");

				if (_mapUserImages[5, 2] != newValue)
				{
					_mapUserImages[5, 2] = newValue;
				}
			}
		}

		public string UserMapImage_207
		{
			get
			{
				return _mapUserImages[6, 1];
			}
			set
			{
				if ((_mapUserImages[6, 1] == null && value != null)
					|| (_mapUserImages[6, 1] != null && !_mapUserImages[6, 1].Equals(value)))
				{
					_mapUserImages[6, 1] = value;
				}
			}
		}

		public string UserMapName_207
		{
			get
			{
				return _mapUserImages[6, 3];
			}
			set
			{
				if ((_mapUserImages[6, 3] == null && value != null)
					|| (_mapUserImages[6, 3] != null && !_mapUserImages[6, 3].Equals(value)))
				{
					_mapUserImages[6, 3] = value;
				}
			}
		}

		public bool UserMapSize_207
		{
			get
			{
				return Convert.ToBoolean(_mapUserImages[6, 2] == "2" ? "True" : "False");
			}
			set
			{
				string newValue = (value ? "2" : "1");

				if (_mapUserImages[6, 2] != newValue)
				{
					_mapUserImages[6, 2] = newValue;
				}
			}
		}

		public string UserMapImage_208
		{
			get
			{
				return _mapUserImages[7, 1];
			}
			set
			{
				if ((_mapUserImages[7, 1] == null && value != null)
					|| (_mapUserImages[7, 1] != null && !_mapUserImages[7, 1].Equals(value)))
				{
					_mapUserImages[7, 1] = value;
				}
			}
		}

		public string UserMapName_208
		{
			get
			{
				return _mapUserImages[7, 3];
			}
			set
			{
				if ((_mapUserImages[7, 3] == null && value != null)
					|| (_mapUserImages[7, 3] != null && !_mapUserImages[7, 3].Equals(value)))
				{
					_mapUserImages[7, 3] = value;
				}
			}
		}

		public bool UserMapSize_208
		{
			get
			{
				return Convert.ToBoolean(_mapUserImages[7, 2] == "2" ? "True" : "False");
			}
			set
			{
				string newValue = (value ? "2" : "1");

				if (_mapUserImages[7, 2] != newValue)
				{
					_mapUserImages[7, 2] = newValue;
				}
			}
		}

		public string BaseMapName_101
		{
			get
			{
				return _mapBaseImages[0, 3];
			}
			set
			{
				if ((_mapBaseImages[0, 3] == null && value != null)
					|| (_mapBaseImages[0, 3] != null && !_mapBaseImages[0, 3].Equals(value)))
				{
					_mapBaseImages[0, 3] = value;
				}
			}
		}

		public string BaseMapName_102
		{
			get
			{
				return _mapBaseImages[1, 3];
			}
			set
			{
				if ((_mapBaseImages[1, 3] == null && value != null)
					|| (_mapBaseImages[1, 3] != null && !_mapBaseImages[1, 3].Equals(value)))
				{
					_mapBaseImages[1, 3] = value;
				}
			}
		}

		public string BaseMapName_103
		{
			get
			{
				return _mapBaseImages[2, 3];
			}
			set
			{
				if ((_mapBaseImages[2, 3] == null && value != null)
					|| (_mapBaseImages[2, 3] != null && !_mapBaseImages[2, 3].Equals(value)))
				{
					_mapBaseImages[2, 3] = value;
				}
			}
		}

		public string BaseMapName_104
		{
			get
			{
				return _mapBaseImages[3, 3];
			}
			set
			{
				if ((_mapBaseImages[3, 3] == null && value != null)
					|| (_mapBaseImages[3, 3] != null && !_mapBaseImages[3, 3].Equals(value)))
				{
					_mapBaseImages[3, 3] = value;
				}
			}
		}

		public string BaseMapName_105
		{
			get
			{
				return _mapBaseImages[4, 3];
			}
			set
			{
				if ((_mapBaseImages[4, 3] == null && value != null)
					|| (_mapBaseImages[4, 3] != null && !_mapBaseImages[4, 3].Equals(value)))
				{
					_mapBaseImages[4, 3] = value;
				}
			}
		}

		public string BaseMapName_106
		{
			get
			{
				return _mapBaseImages[5, 3];
			}
			set
			{
				if ((_mapBaseImages[5, 3] == null && value != null)
					|| (_mapBaseImages[5, 3] != null && !_mapBaseImages[5, 3].Equals(value)))
				{
					_mapBaseImages[5, 3] = value;
				}
			}
		}

		public string BaseMapName_107
		{
			get
			{
				return _mapBaseImages[6, 3];
			}
			set
			{
				if ((_mapBaseImages[6, 3] == null && value != null)
					|| (_mapBaseImages[6, 3] != null && !_mapBaseImages[6, 3].Equals(value)))
				{
					_mapBaseImages[6, 3] = value;
				}
			}
		}

		public string BaseMapName_108
		{
			get
			{
				return _mapBaseImages[7, 3];
			}
			set
			{
				if ((_mapBaseImages[7, 3] == null && value != null)
					|| (_mapBaseImages[7, 3] != null && !_mapBaseImages[7, 3].Equals(value)))
				{
					_mapBaseImages[7, 3] = value;
				}
			}
		}

		public string BaseMapName_109
		{
			get
			{
				return _mapBaseImages[8, 3];
			}
			set
			{
				if ((_mapBaseImages[8, 3] == null && value != null)
					|| (_mapBaseImages[8, 3] != null && !_mapBaseImages[8, 3].Equals(value)))
				{
					_mapBaseImages[8, 3] = value;
				}
			}
		}

		public string BaseMapName_110
		{
			get
			{
				return _mapBaseImages[9, 3];
			}
			set
			{
				if ((_mapBaseImages[9, 3] == null && value != null)
					|| (_mapBaseImages[9, 3] != null && !_mapBaseImages[9, 3].Equals(value)))
				{
					_mapBaseImages[9, 3] = value;
				}
			}
		}

		public string BaseMapName_111
		{
			get
			{
				return _mapBaseImages[10, 3];
			}
			set
			{
				if ((_mapBaseImages[10, 3] == null && value != null)
					|| (_mapBaseImages[10, 3] != null && !_mapBaseImages[10, 3].Equals(value)))
				{
					_mapBaseImages[10, 3] = value;
				}
			}
		}

		public string BaseMapName_112
		{
			get
			{
				return _mapBaseImages[11, 3];
			}
			set
			{
				if ((_mapBaseImages[11, 3] == null && value != null)
					|| (_mapBaseImages[11, 3] != null && !_mapBaseImages[11, 3].Equals(value)))
				{
					_mapBaseImages[11, 3] = value;
				}
			}
		}

		public string BaseMapName_113
		{
			get
			{
				return _mapBaseImages[12, 3];
			}
			set
			{
				if ((_mapBaseImages[12, 3] == null && value != null)
					|| (_mapBaseImages[12, 3] != null && !_mapBaseImages[12, 3].Equals(value)))
				{
					_mapBaseImages[12, 3] = value;
				}
			}
		}

		public string BaseMapName_114
		{
			get
			{
				return _mapBaseImages[13, 3];
			}
			set
			{
				if ((_mapBaseImages[13, 3] == null && value != null)
					|| (_mapBaseImages[13, 3] != null && !_mapBaseImages[13, 3].Equals(value)))
				{
					_mapBaseImages[13, 3] = value;
				}
			}
		}

		public string BaseMapName_115
		{
			get
			{
				return _mapBaseImages[14, 3];
			}
			set
			{
				if ((_mapBaseImages[14, 3] == null && value != null)
					|| (_mapBaseImages[14, 3] != null && !_mapBaseImages[14, 3].Equals(value)))
				{
					_mapBaseImages[14, 3] = value;
				}
			}
		}

		public string BaseMapName_116
		{
			get
			{
				return _mapBaseImages[15, 3];
			}
			set
			{
				if ((_mapBaseImages[15, 3] == null && value != null)
					|| (_mapBaseImages[15, 3] != null && !_mapBaseImages[15, 3].Equals(value)))
				{
					_mapBaseImages[15, 3] = value;
				}
			}
		}

		public string BaseMapName_117
		{
			get
			{
				return _mapBaseImages[16, 3];
			}
			set
			{
				if ((_mapBaseImages[16, 3] == null && value != null)
					|| (_mapBaseImages[16, 3] != null && !_mapBaseImages[16, 3].Equals(value)))
				{
					_mapBaseImages[16, 3] = value;
				}
			}
		}

		public string BaseMapName_118
		{
			get
			{
				return _mapBaseImages[17, 3];
			}
			set
			{
				if ((_mapBaseImages[17, 3] == null && value != null)
					|| (_mapBaseImages[17, 3] != null && !_mapBaseImages[17, 3].Equals(value)))
				{
					_mapBaseImages[17, 3] = value;
				}
			}
		}

		public string BaseMapName_119
		{
			get
			{
				return _mapBaseImages[18, 3];
			}
			set
			{
				if ((_mapBaseImages[18, 3] == null && value != null)
					|| (_mapBaseImages[18, 3] != null && !_mapBaseImages[18, 3].Equals(value)))
				{
					_mapBaseImages[18, 3] = value;
				}
			}
		}

		public string BaseMapName_120
		{
			get
			{
				return _mapBaseImages[19, 3];
			}
			set
			{
				if ((_mapBaseImages[19, 3] == null && value != null)
					|| (_mapBaseImages[19, 3] != null && !_mapBaseImages[19, 3].Equals(value)))
				{
					_mapBaseImages[19, 3] = value;
				}
			}
		}

		public string BaseMapName_121
		{
			get
			{
				return _mapBaseImages[20, 3];
			}
			set
			{
				if ((_mapBaseImages[20, 3] == null && value != null)
					|| (_mapBaseImages[20, 3] != null && !_mapBaseImages[20, 3].Equals(value)))
				{
					_mapBaseImages[20, 3] = value;
				}
			}
		}

		public string BaseMapName_122
		{
			get
			{
				return _mapBaseImages[21, 3];
			}
			set
			{
				if ((_mapBaseImages[21, 3] == null && value != null)
					|| (_mapBaseImages[21, 3] != null && !_mapBaseImages[21, 3].Equals(value)))
				{
					_mapBaseImages[21, 3] = value;
				}
			}
		}

		public string BaseMapName_123
		{
			get
			{
				return _mapBaseImages[22, 3];
			}
			set
			{
				if ((_mapBaseImages[22, 3] == null && value != null)
					|| (_mapBaseImages[22, 3] != null && !_mapBaseImages[22, 3].Equals(value)))
				{
					_mapBaseImages[22, 3] = value;
				}
			}
		}

		public string BaseMapName_124
		{
			get
			{
				return _mapBaseImages[23, 3];
			}
			set
			{
				if ((_mapBaseImages[23, 3] == null && value != null)
					|| (_mapBaseImages[23, 3] != null && !_mapBaseImages[23, 3].Equals(value)))
				{
					_mapBaseImages[23, 3] = value;
				}
			}
		}

		#endregion


		#region Read/Write Xml

		public override void WriteXml(XmlWriter writer)
		{
			base.WriteXml(writer);

			writer.WriteElementString("UserMapImage_201", UserMapImage_201);
			writer.WriteElementString("UserMapName_201", UserMapName_201);
			writer.WriteElementString("UserMapSize_201", UserMapSize_201.ToString(CultureInfo.InvariantCulture));
			writer.WriteElementString("UserMapImage_202", UserMapImage_202);
			writer.WriteElementString("UserMapName_202", UserMapName_202);
			writer.WriteElementString("UserMapSize_202", UserMapSize_202.ToString(CultureInfo.InvariantCulture));
			writer.WriteElementString("UserMapImage_203", UserMapImage_203);
			writer.WriteElementString("UserMapName_203", UserMapName_203);
			writer.WriteElementString("UserMapSize_203", UserMapSize_203.ToString(CultureInfo.InvariantCulture));
			writer.WriteElementString("UserMapImage_204", UserMapImage_204);
			writer.WriteElementString("UserMapName_204", UserMapName_204);
			writer.WriteElementString("UserMapSize_204", UserMapSize_204.ToString(CultureInfo.InvariantCulture));
			writer.WriteElementString("UserMapImage_205", UserMapImage_205);
			writer.WriteElementString("UserMapName_205", UserMapName_205);
			writer.WriteElementString("UserMapSize_205", UserMapSize_205.ToString(CultureInfo.InvariantCulture));
			writer.WriteElementString("UserMapImage_206", UserMapImage_206);
			writer.WriteElementString("UserMapName_206", UserMapName_206);
			writer.WriteElementString("UserMapSize_206", UserMapSize_206.ToString(CultureInfo.InvariantCulture));
			writer.WriteElementString("UserMapImage_207", UserMapImage_207);
			writer.WriteElementString("UserMapName_207", UserMapName_207);
			writer.WriteElementString("UserMapSize_207", UserMapSize_207.ToString(CultureInfo.InvariantCulture));
			writer.WriteElementString("UserMapImage_208", UserMapImage_208);
			writer.WriteElementString("UserMapName_208", UserMapName_208);
			writer.WriteElementString("UserMapSize_208", UserMapSize_208.ToString(CultureInfo.InvariantCulture));

			writer.WriteElementString("BaseMapName_101", BaseMapName_101);
			writer.WriteElementString("BaseMapName_102", BaseMapName_102);
			writer.WriteElementString("BaseMapName_103", BaseMapName_103);
			writer.WriteElementString("BaseMapName_104", BaseMapName_104);
			writer.WriteElementString("BaseMapName_105", BaseMapName_105);
			writer.WriteElementString("BaseMapName_106", BaseMapName_106);
			writer.WriteElementString("BaseMapName_107", BaseMapName_107);
			writer.WriteElementString("BaseMapName_108", BaseMapName_108);
			writer.WriteElementString("BaseMapName_109", BaseMapName_109);
			writer.WriteElementString("BaseMapName_110", BaseMapName_110);
			writer.WriteElementString("BaseMapName_111", BaseMapName_111);
			writer.WriteElementString("BaseMapName_112", BaseMapName_112);
			writer.WriteElementString("BaseMapName_113", BaseMapName_113);
			writer.WriteElementString("BaseMapName_114", BaseMapName_114);
			writer.WriteElementString("BaseMapName_115", BaseMapName_115);
			writer.WriteElementString("BaseMapName_116", BaseMapName_116);
			writer.WriteElementString("BaseMapName_117", BaseMapName_117);
			writer.WriteElementString("BaseMapName_118", BaseMapName_118);
			writer.WriteElementString("BaseMapName_119", BaseMapName_119);
			writer.WriteElementString("BaseMapName_120", BaseMapName_120);
			writer.WriteElementString("BaseMapName_121", BaseMapName_121);
			writer.WriteElementString("BaseMapName_122", BaseMapName_122);
			writer.WriteElementString("BaseMapName_123", BaseMapName_123);
			writer.WriteElementString("BaseMapName_124", BaseMapName_124);
		}

		public override void ReadXml(XmlReader reader)
		{
			base.ReadXml(reader);

			UserMapImage_201 = reader.ReadElementString("UserMapImage_201");
			UserMapName_201 = reader.ReadElementString("UserMapName_201");
			UserMapSize_201 = bool.Parse(reader.ReadElementString("UserMapSize_201"));
			UserMapImage_202 = reader.ReadElementString("UserMapImage_202");
			UserMapName_202 = reader.ReadElementString("UserMapName_202");
			UserMapSize_202 = bool.Parse(reader.ReadElementString("UserMapSize_202"));
			UserMapImage_203 = reader.ReadElementString("UserMapImage_203");
			UserMapName_203 = reader.ReadElementString("UserMapName_203");
			UserMapSize_203 = bool.Parse(reader.ReadElementString("UserMapSize_203"));
			UserMapImage_204 = reader.ReadElementString("UserMapImage_204");
			UserMapName_204 = reader.ReadElementString("UserMapName_204");
			UserMapSize_204 = bool.Parse(reader.ReadElementString("UserMapSize_204"));
			UserMapImage_205 = reader.ReadElementString("UserMapImage_205");
			UserMapName_205 = reader.ReadElementString("UserMapName_205");
			UserMapSize_205 = bool.Parse(reader.ReadElementString("UserMapSize_205"));
			UserMapImage_206 = reader.ReadElementString("UserMapImage_206");
			UserMapName_206 = reader.ReadElementString("UserMapName_206");
			UserMapSize_206 = bool.Parse(reader.ReadElementString("UserMapSize_206"));
			UserMapImage_207 = reader.ReadElementString("UserMapImage_207");
			UserMapName_207 = reader.ReadElementString("UserMapName_207");
			UserMapSize_207 = bool.Parse(reader.ReadElementString("UserMapSize_207"));
			UserMapImage_208 = reader.ReadElementString("UserMapImage_208");
			UserMapName_208 = reader.ReadElementString("UserMapName_208");
			UserMapSize_208 = bool.Parse(reader.ReadElementString("UserMapSize_208"));

			BaseMapName_101 = reader.ReadElementString("BaseMapName_101");
			BaseMapName_102 = reader.ReadElementString("BaseMapName_102");
			BaseMapName_103 = reader.ReadElementString("BaseMapName_103");
			BaseMapName_104 = reader.ReadElementString("BaseMapName_104");
			BaseMapName_105 = reader.ReadElementString("BaseMapName_105");
			BaseMapName_106 = reader.ReadElementString("BaseMapName_106");
			BaseMapName_107 = reader.ReadElementString("BaseMapName_107");
			BaseMapName_108 = reader.ReadElementString("BaseMapName_108");
			BaseMapName_109 = reader.ReadElementString("BaseMapName_109");
			BaseMapName_110 = reader.ReadElementString("BaseMapName_110");
			BaseMapName_111 = reader.ReadElementString("BaseMapName_111");
			BaseMapName_112 = reader.ReadElementString("BaseMapName_112");
			BaseMapName_113 = reader.ReadElementString("BaseMapName_113");
			BaseMapName_114 = reader.ReadElementString("BaseMapName_114");
			BaseMapName_115 = reader.ReadElementString("BaseMapName_115");
			BaseMapName_116 = reader.ReadElementString("BaseMapName_116");
			BaseMapName_117 = reader.ReadElementString("BaseMapName_117");
			BaseMapName_118 = reader.ReadElementString("BaseMapName_118");
			BaseMapName_119 = reader.ReadElementString("BaseMapName_119");
			BaseMapName_120 = reader.ReadElementString("BaseMapName_120");
			BaseMapName_121 = reader.ReadElementString("BaseMapName_121");
			BaseMapName_122 = reader.ReadElementString("BaseMapName_122");
			BaseMapName_123 = reader.ReadElementString("BaseMapName_123");
			BaseMapName_124 = reader.ReadElementString("BaseMapName_124");

			Refresh();
		}

		#endregion

	}
}
