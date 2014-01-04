using System;

using Sce.PlayStation.Core;
using Sce.PlayStation.Core.Graphics;
using Sce.PlayStation.Core.Audio;

using Sce.PlayStation.HighLevel.GameEngine2D;
using Sce.PlayStation.HighLevel.GameEngine2D.Base;

namespace shooting1
{
	// タイトル画面
	public class TitleScreen
	{
		static TextureInfo sky_texture;
		static TextureInfo title_texture;
		
		static Bgm bgm;
		static BgmPlayer bgm_player;

		// シーンの作成
		public static Scene CreateScene()
		{
			Console.WriteLine("creating TitleScreen");
	
			// Bgmを再生する
			bgm = new Bgm( "Application/sounds/title.mp3" );
			bgm_player = bgm.CreatePlayer();
			bgm_player.Play();

			var scene = new Scene(){ Name = "TitleScene" };
			
			scene.OnExitEvents += DisposeScene;

			// set the camera so that the part of the word we see on screen matches in screen coordinates
			scene.Camera.SetViewFromViewport();
	
			// create a new TextureInfo object, used by sprite primitives
			sky_texture = new TextureInfo( new Texture2D("/Application/textures/sky.png", false ) );
			title_texture = new TextureInfo( new Texture2D("/Application/textures/title.png", false ) );
	
			var background_sprite = new SpriteUV() { TextureInfo = sky_texture };
			background_sprite.Quad.S = sky_texture.TextureSizef;
			background_sprite.CenterSprite();
			background_sprite.Position = scene.Camera.CalcBounds().Center;
			scene.AddChild( background_sprite );
			
			var title_sprite = new SpriteUV() { TextureInfo = title_texture };
			title_sprite.Quad.S = title_texture.TextureSizef;
			title_sprite.CenterSprite();
			title_sprite.Position = scene.Camera.CalcBounds().Center;
			scene.AddChild( title_sprite );
			
			scene.Schedule( (dt) =>
			{
				var touch_data = Input2.Touch.GetData(0);
				
				for( int i=0 ; i<touch_data.Length ; ++i )
				{
					if( touch_data[i].Press )
					{
						GotoGameScreen();
					}
				}
			});
			
			return scene;
		}
		
		// 画面切り替え前の停止処理
		static void Stop()
		{
			bgm_player.Stop();
			bgm_player.Dispose();
		}
		
		// 画面の廃棄
		static void DisposeScene()
		{
			sky_texture.Dispose();
			title_texture.Dispose();
			
			bgm.Dispose();
		}
		
		// ゲーム画面に遷移
		static void GotoGameScreen()
		{
			Stop();
			
			var next_scene = GameScreen.CreateScene();
			
			Director.Instance.ReplaceScene( next_scene );
		}
	}
}
