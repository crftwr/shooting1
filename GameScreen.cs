using System;
using System.IO;
using System.Text;
using System.Json;

using Sce.PlayStation.Core;
using Sce.PlayStation.Core.Graphics;
using Sce.PlayStation.Core.Audio;

using Sce.PlayStation.HighLevel.GameEngine2D;
using Sce.PlayStation.HighLevel.GameEngine2D.Base;

namespace shooting1
{
	// ゲーム画面
	public static class GameScreen
	{
		public static TextureInfo background_texture;
		public static TextureInfo player_texture;
		public static TextureInfo bullet_texture;

		static Bgm bgm;
		static BgmPlayer bgm_player;
		
		public static Player player;
		public static SpriteList bulletList;
		
		static JsonArray leveldata;
		static int leveldata_index;
		static float leveldata_time;

		// シーンの作成
		public static Scene CreateScene()
		{
			Console.WriteLine("creating GameScreen");
			
			// Bgmを再生する
			bgm = new Bgm( "Application/sounds/game.mp3" );
			bgm_player = bgm.CreatePlayer();
			bgm_player.Loop = true;
			bgm_player.Play();

			var scene = new Scene(){ Name = "GameScene" };
			
			scene.OnExitEvents += DisposeScene;

			// set the camera so that the part of the word we see on screen matches in screen coordinates
			scene.Camera.SetViewFromViewport();
	
			// テクスチャロード
			background_texture = new TextureInfo( new Texture2D("/Application/textures/background.png", false ) );
			player_texture = new TextureInfo( new Texture2D("/Application/textures/player.png", false ) );
			bullet_texture = new TextureInfo( new Texture2D("/Application/textures/bullet.png", false ) );
	
			// 背景
			var background = new Background(background_texture);
			background.Position = scene.Camera.CalcBounds().Center;
			scene.AddChild( background );

			// プレイヤーキャラクタ
			player = new Player(player_texture);
			player.Position = new Vector2(480,100);
			scene.AddChild( player );
			
			// 弾丸リスト
			bulletList = new SpriteList(bullet_texture);
			scene.AddChild( bulletList );

			// 面データのロード
			{
				StreamReader sr = new StreamReader( "/Application/jsons/level1.json", Encoding.GetEncoding("utf-8") );
				var json_string = sr.ReadToEnd();

				leveldata = (JsonArray)JsonValue.Parse(json_string);
				leveldata_index = 0;
				leveldata_time = 0.0f;
            }
			
			// タイムライン処理
			scene.Schedule( (delta_time) =>
			{
				var touch_data = Input2.Touch.GetData(0);
				for( int i=0 ; i<touch_data.Length ; ++i )
				{
					if( touch_data[i].Press )
					{
						GotoTitleScreen();
					}
				}
				
				leveldata_time += delta_time;
				while( leveldata_index < leveldata.Count )
				{
					var leveldata_item = (JsonObject)leveldata[leveldata_index];
					
					float time = leveldata_item.GetValue("time").ReadAs<float>();
					if(time>leveldata_time){ break; }
					
					string type = leveldata_item.GetValue("type").ReadAs<string>();
					
					switch(type)
					{
					case "enemy1":
						{
							Console.WriteLine("enemy1");
						}
						break;
						
					case "goal":
						{
							Console.WriteLine("goal");
						}
						break;
					}
					
					leveldata_index++;
				}
			});
			
			// 弾丸の一括処理
			bulletList.Schedule( (delta_time) =>
           	{
				for( int i=0 ; i<bulletList.Children.Count ; )
				{
					Bullet bullet = (Bullet)bulletList.Children[i];
					
					bullet.Position += bullet.speed;
					
					if( bullet.Position.X < -100.0f || bullet.Position.X > 960.0f+100.0f
					 || bullet.Position.Y < -100.0f || bullet.Position.Y > 544.0f+100.0f )
					{
						bulletList.Children.RemoveAt(i);
						continue;
					}
					
					++i;
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
			background_texture.Dispose();
			player_texture.Dispose();
			bullet_texture.Dispose();

			bgm.Dispose();
		}

		// タイトル画面に遷移
		static void GotoTitleScreen()
		{
			Stop();
			
			var next_scene = TitleScreen.CreateScene();
			
			Director.Instance.ReplaceScene( next_scene );
		}
	}

	// 背景
	public class Background : SpriteUV
	{
		public Background( TextureInfo texture_info )
			: base(texture_info)
		{
			// make the texture 1:1 on screen
			Quad.S = texture_info.TextureSizef;
	
			// center the sprite around its own .Position 
			// (by default .Position is the lower left bit of the sprite)
			CenterSprite();
		}
	}
	
	// プレイヤキャラクタ
	public class Player : SpriteUV
	{
		const float ground_left_accel = -0.5f;
		const float ground_right_accel = 0.7f;
		const float air_left_accel = -0.05f;
		const float air_right_accel = 0.05f;
		const float fly_accel = 1.0f;
		const float gravity = -0.7f;
		static Vector2 air_friction = new Vector2(0.98f,0.98f);
		static Vector2 ground_friction = new Vector2(0.80f,1.0f);
		
		const float ground_level = 100.0f;
		
		Vector2 speed = new Vector2(0,0);
		bool flying = false;
		const float fire_interval = 0.2f;
		float fire_time = 0.0f;
		const float fly_max_time = 1.0f;
		float fly_time = fly_max_time;
		
		public Player( TextureInfo texture_info )
			: base(texture_info)
		{
			// make the texture 1:1 on screen
			Quad.S = texture_info.TextureSizef;
	
			// center the sprite around its own .Position 
			// (by default .Position is the lower left bit of the sprite)
			CenterSprite();
	
			// プレイヤの毎フレーム処理		
			this.Schedule( (delta_time) => 
			{
				var pad = Input2.GamePad.GetData(0);
				
				// 左右移動
				if(pad.Left.Down)
				{
					if(flying)
					{
						speed.X += air_left_accel;
					}
					else
					{
						speed.X += ground_left_accel;
					}
				}
				else if(pad.Right.Down)
				{
					if(flying)
					{
						speed.X += air_right_accel;
					}
					else
					{
						speed.X += ground_right_accel;
					}
				}
				
				// 飛行
				if(pad.Up.Down)
				{
					if( fly_time > 0.0f )
					{
						speed.Y += fly_accel;
						fly_time -= delta_time;
					}
				}
				if(!flying)
				{
					fly_time = fly_max_time;
				}
				
				// 抵抗
				if(flying)
				{
					speed *= air_friction;
				}
				else
				{
					speed *= ground_friction;
				}

				// 重力
				speed.Y += gravity;
				
				// 加算
				Position += speed;

				// 地面との衝突
				if( Position.Y > ground_level )
				{
					flying = true;
				}
				else
				{
					flying = false;

					speed.Y = 0.0f;
					Position = new Vector2(Position.X,100.0f);
				}
				
				// 弾丸発射
				if( fire_time <= 0.0f )
				{
					if(pad.AnalogRight.LengthSquared() > 0.2f
					 && pad.AnalogRight.X > -0.5f
					 && pad.AnalogRight.Y < 0.5f )
					{
						var fire_direction = pad.AnalogRight * new Vector2(1,-1);
						if(fire_direction.X < 0.0f)
						{
							fire_direction.X = 0.0f;
						}
						if(fire_direction.Y < 0.0f)
						{
							fire_direction.Y = 0.0f;
						}
						fire_direction = fire_direction.Normalize();
						
						FireBullet(ref fire_direction);
						
						fire_time = fire_interval;
					}
				}
				else
				{
					fire_time -= delta_time;
				}
			});
		}
		
		// 弾丸発射
		public void FireBullet( ref Vector2 direction )
		{
			Console.WriteLine("Fire!" + GameScreen.bulletList.Children.Count);
			
			var position = Position + new Vector2(50,0);
			
			var bullet = new Bullet( ref position, ref direction ); 
			
			GameScreen.bulletList.AddChild(bullet);
		}
	}

	// 弾丸
	public class Bullet : SpriteUV
	{
		const float ground_level = 100.0f;
		
		public Vector2 speed = new Vector2(30,0);
		
		public Bullet( ref Vector2 position, ref Vector2 direction )
			: base()
		{
			Position = position;
			speed = direction * 10;
			
			Rotation = direction;
			
			// make the texture 1:1 on screen
			Quad.S = GameScreen.bullet_texture.TextureSizef;
	
			// center the sprite around its own .Position 
			// (by default .Position is the lower left bit of the sprite)
			CenterSprite();
		}
	}
}

