using System;
using System.Collections.Generic;

using Sce.PlayStation.Core;
using Sce.PlayStation.Core.Environment;
using Sce.PlayStation.Core.Graphics;
using Sce.PlayStation.Core.Input;

using Sce.PlayStation.HighLevel.GameEngine2D;
using Sce.PlayStation.HighLevel.GameEngine2D.Base;

namespace shooting1
{
	public class AppMain
	{
		public static void Main (string[] args)
		{
			Director.Initialize();
		
			Director.Instance.GL.Context.SetClearColor( Colors.Grey20 );
	
			// set debug flags that display rulers to debug coordinates
			// Director.Instance.DebugFlags |= DebugFlags.DrawGrid;
			// set the camera navigation debug flag (press left alt + mouse to navigate in 2d space)
			Director.Instance.DebugFlags |= DebugFlags.Navigate; 
	
			var game_scene = GameScreen.CreateScene();
			
			Director.Instance.RunWithScene( game_scene );

			Director.Terminate();
		}
	}
}
