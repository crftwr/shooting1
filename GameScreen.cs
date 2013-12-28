using System;

using Sce.PlayStation.Core;
using Sce.PlayStation.Core.Graphics;
using Sce.PlayStation.Core.Audio;

using Sce.PlayStation.HighLevel.GameEngine2D;
using Sce.PlayStation.HighLevel.GameEngine2D.Base;

namespace shooting1
{
	public static class GameScreen
	{
		static TextureInfo background_texture;

		static Bgm bgm;
		static BgmPlayer bgm_player;

		public static Scene CreateScene()
		{
			Console.WriteLine("creating GameScreen");
			
			// Bgmを再生する
			bgm = new Bgm( "Application/sounds/game.mp3" );
			bgm_player = bgm.CreatePlayer();
			bgm_player.Play();

			var scene = new Scene(){ Name = "GameScene" };
			
			scene.OnExitEvents += DisposeScene;

			// set the camera so that the part of the word we see on screen matches in screen coordinates
			scene.Camera.SetViewFromViewport();
	
			// create a new TextureInfo object, used by sprite primitives
			background_texture = new TextureInfo( new Texture2D("/Application/textures/background.png", false ) );
	
			// create a new sprite
			var sprite = new SpriteUV() { TextureInfo = background_texture };
	
			// make the texture 1:1 on screen
			sprite.Quad.S = background_texture.TextureSizef;
	
			// center the sprite around its own .Position 
			// (by default .Position is the lower left bit of the sprite)
			sprite.CenterSprite();
	
			// put the sprite at the center of the screen
			sprite.Position = scene.Camera.CalcBounds().Center;
	
			// our scene only has 2 nodes: scene->sprite
			scene.AddChild( sprite );
			
			scene.Schedule( (dt) =>
			{
				var touch_data = Input2.Touch.GetData(0);
				
				for( int i=0 ; i<touch_data.Length ; ++i )
				{
					if( touch_data[i].Press )
					{
						var next_scene = TitleScreen.CreateScene();
						
						Director.Instance.ReplaceScene( next_scene );
					}
				}
			});
			
			return scene;
		}
		
		static void DisposeScene()
		{
			background_texture.Dispose();

			bgm_player.Dispose();
			bgm.Dispose();
		}
	}
}

