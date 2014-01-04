using System;
using System.IO;
using System.Text;
using System.Json;
using System.Diagnostics;

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
		public static TextureInfo sky_texture;
		public static TextureInfo far_texture;
		public static TextureInfo ground_texture;
		public static TextureInfo player_texture;
		public static TextureInfo bullet_texture;
		public static TextureInfo enemy1_texture;
		public static TextureInfo enemy2_texture;
		public static TextureInfo fire1_texture;

		static Bgm bgm;
		static BgmPlayer bgm_player;
		
		public static Scene scene;
		public static Player player;
		public static SpriteList bulletList;
		public static SpriteList enemy1List;
		public static SpriteList enemy2List;
		
		static float scroll_position;
		const float scroll_speed = 100.0f;
		
		static JsonArray events;
		static int events_index;
		
		static float [] hightmap;
		const int hightmap_granularity = 16;
		
		public const float collision_distance_enemy1_bullet2 = 40.0f * 40.0f;
		public const float collision_distance_enemy2_bullet2 = 40.0f * 40.0f;
		public const float collision_distance_enemy1_player2 = 40.0f * 40.0f;
		public const float collision_distance_enemy2_player2 = 40.0f * 40.0f;

		// シーンの作成
		public static Scene CreateScene()
		{
			Console.WriteLine("creating GameScreen");
			
			// Bgmを再生する
			bgm = new Bgm( "Application/sounds/game.mp3" );
			bgm_player = bgm.CreatePlayer();
			bgm_player.Loop = true;
			bgm_player.Play();

			scene = new Scene(){ Name = "GameScene" };
			
			scene.OnExitEvents += DisposeScene;

			// set the camera so that the part of the word we see on screen matches in screen coordinates
			scene.Camera.SetViewFromViewport();
	
			// テクスチャロード
			sky_texture = new TextureInfo( new Texture2D("/Application/textures/sky.png", false ) );
			far_texture = new TextureInfo( new Texture2D("/Application/textures/far.png", false ) );
			ground_texture = new TextureInfo( new Texture2D("/Application/textures/ground.png", false ) );
			player_texture = new TextureInfo( new Texture2D("/Application/textures/player.png", false ) );
			bullet_texture = new TextureInfo( new Texture2D("/Application/textures/bullet.png", false ) );
			enemy1_texture = new TextureInfo( new Texture2D("/Application/textures/enemy1.png", false ) );
			enemy2_texture = new TextureInfo( (Texture2D)enemy1_texture.Texture.ShallowClone() );
			fire1_texture = new TextureInfo( new Texture2D("/Application/textures/fire1.png", false ) );
	
			// 背景
			var background = new Background( sky_texture, far_texture, ground_texture );
			//background.Position = scene.Camera.CalcBounds().Center;
			scene.AddChild( background );

			// プレイヤーキャラクタ
			player = new Player(player_texture);
			player.Position = new Vector2(480,100);
			scene.AddChild( player );
			
			// 弾丸リスト
			bulletList = new SpriteList(bullet_texture);
			scene.AddChild( bulletList );

			// 敵リスト
			enemy1List = new SpriteList(enemy1_texture);
			scene.AddChild( enemy1List );
			enemy2List = new SpriteList(enemy2_texture);
			scene.AddChild( enemy2List );

			// 面データのロード
			{
				StreamReader sr = new StreamReader( "/Application/jsons/level1.json", Encoding.GetEncoding("utf-8") );
				var json_string = sr.ReadToEnd();

				var leveldata = (JsonObject)JsonValue.Parse(json_string);
				
				var hights = (JsonArray)leveldata.GetValue("hight");
				PrepareHightmap(hights);
				
				events = (JsonArray)leveldata.GetValue("events");
				events_index = 0;
            }
	
			// 毎フレーム処理
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
				
				// 背景スクロール
				scroll_position += scroll_speed * delta_time;
				background.Scroll( scroll_position );
				
				// タイムライン処理
				while( events_index < events.Count )
				{
					var leveldata_item = (JsonObject)events[events_index];
					
					float scroll = leveldata_item.GetValue("scroll").ReadAs<float>();
					if(scroll>scroll_position){ break; }
					
					string type = leveldata_item.GetValue("type").ReadAs<string>();
					
					switch(type)
					{
					case "enemy1":
						{
							Console.WriteLine("enemy1");
		
							float x = leveldata_item.GetValue("x").ReadAs<float>();
							float y = leveldata_item.GetValue("y").ReadAs<float>();
							var position = new Vector2(x,y);
						
							CreateEnemy1( ref position );
						}
						break;
						
					case "enemy2":
						{
							Console.WriteLine("enemy2");
		
							float x = leveldata_item.GetValue("x").ReadAs<float>();
							float y = leveldata_item.GetValue("y").ReadAs<float>();
							var position = new Vector2(x,y);
						
							CreateEnemy2( ref position );
						}
						break;
						
					case "goal":
						{
							Console.WriteLine("goal");
						}
						break;
						
					default:
						//Debug.Assert(false); // 効かない、、、
						break;
					
					}
					
					events_index++;
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
						bulletList.RemoveChild(bullet,true);
						continue;
					}
					
					// 敵と弾丸の衝突
					{
						Func<SpriteList,float,bool> HitTestEnemyBullet = (enemyList,distance2) =>
						{
							for( int enemy_index=0 ; enemy_index<enemyList.Children.Count; ++enemy_index )
							{
								var enemy = (Enemy)enemyList.Children[enemy_index];
								
								if( Vector2.DistanceSquared( enemy.Position, bullet.Position ) < distance2 )
								{
									var position = enemy.Position;
									CreateExplosion1(ref position);
									
									enemyList.RemoveChild(enemy,true);
									
									return true;
								}
							}
							
							return false;
						};
						
						bool hit = false;
						if(!hit){ hit = HitTestEnemyBullet(enemy1List,collision_distance_enemy1_bullet2); }
						if(!hit){ hit = HitTestEnemyBullet(enemy2List,collision_distance_enemy2_bullet2); }
						if(hit)
						{
							bulletList.Children.RemoveAt(i);
							continue;
						}
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
			sky_texture.Dispose();
			far_texture.Dispose();
			ground_texture.Dispose();
			player_texture.Dispose();
			bullet_texture.Dispose();
			enemy1_texture.Dispose();
			fire1_texture.Dispose();

			bgm.Dispose();
		}

		// タイトル画面に遷移
		static void GotoTitleScreen()
		{
			Stop();
			
			var next_scene = TitleScreen.CreateScene();
			
			Director.Instance.ReplaceScene( next_scene );
		}
		
		static void PrepareHightmap( JsonArray hight_array )
		{
			hightmap = new float[960 * 10 / hightmap_granularity];
			
			for( int i=0 ; i<hightmap.Length ; ++i )
			{
				hightmap[i] = 100.0f;
			}
			
			var first_hight = (JsonObject)hight_array[0];
			var prev_position = new Vector2( first_hight.GetValue("x").ReadAs<float>(), first_hight.GetValue("y").ReadAs<float>() );
			
			for( int i=1 ; i<hight_array.Count ; ++i )
			{
				var hight = hight_array[i];
				var position = new Vector2( hight.GetValue("x").ReadAs<float>(), hight.GetValue("y").ReadAs<float>() );
				
				for( int x=(int)prev_position.X/hightmap_granularity ; x<(int)position.X/hightmap_granularity ; ++x )
				{
					hightmap[x] = Vector2.Lerp( prev_position, position, ((float)x*hightmap_granularity-prev_position.X)/(position.X-prev_position.X) ).Y;
					
					Console.WriteLine( String.Format("{0},{1}",x,hightmap[x]) );
					
				}

				prev_position = position;
			}
		}
		
		public static float GetHight( float x )
		{
			int i = (int)(x / hightmap_granularity);
			
			if(i+1<hightmap.Length)
			{
				return FMath.Lerp( hightmap[i], hightmap[i+1], (x-i*hightmap_granularity)/hightmap_granularity );
			}
			else
			{
				return 100.0f;
			}
		}
		
		public static float GetHightInScreenSpace( float x )
		{
			return GetHight( x + scroll_position );
		}
		
		static void CreateEnemy1( ref Vector2 position )
		{
			var enemy = new Enemy1(ref position);
			enemy1List.AddChild( enemy );
		}
		
		static void CreateEnemy2( ref Vector2 position )
		{
			var enemy = new Enemy2(ref position);
			enemy2List.AddChild( enemy );
		}
		
		public static void CreateExplosion1( ref Vector2 position )
		{
			Particles fire_node= new Particles(30);
			ParticleSystem fire = fire_node.ParticleSystem;
			fire.TextureInfo = fire1_texture;
	
			fire.Emit.Position = position;
			fire.Emit.PositionVar = new Vector2(30,30);
			fire.Emit.Velocity = new Vector2( 0.0f, 0.0f );
			fire.Emit.VelocityVar = new Vector2( 100f, 100f );
			fire.Emit.ForwardMomentum = 0.0f;
			fire.Emit.AngularMomentun = 0.0f;
			fire.Emit.LifeSpan = 0.5f;
			fire.Emit.LifeSpanRelVar = 0.0f;
			fire.Emit.WaitTime = 0.0f;
			fire.Emit.ColorStart = Colors.Red;
			fire.Emit.ColorStartVar = new Vector4(0.0f,0.0f,0.0f,0.0f);
			fire.Emit.ColorEnd = new Vector4(1.0f,1.0f,1.0f,0.0f);
			fire.Emit.ColorEndVar = new Vector4(0.2f,0.0f,0.0f,0.0f);
			fire.Emit.ScaleStart = 10;
			fire.Emit.ScaleStartRelVar = 3;
			fire.Emit.ScaleEnd = 20;
			fire.Emit.ScaleEndRelVar = 5;
			fire.Simulation.Fade = 0.1f;
			
			// パーティクル消滅処理
			var action = new Sequence();
			action.Add( new DelayTime(0.5f) );
			action.Add( new CallFunc( () => {
				fire_node.Dispose();
				fire_node.Parent.RemoveChild(fire_node,true);
			}));
			fire_node.RunAction(action);
	
			scene.AddChild(fire_node);
		}
	}

	// 背景
	public class Background : SpriteUV
	{
		SpriteUV [] far_layers;
		SpriteUV [] ground_layers;
		
		public Background( TextureInfo sky_texture, TextureInfo far_texture, TextureInfo ground_texture )
			: base(sky_texture)
		{
			Quad.S = new Vector2(960,544);
	
			//CenterSprite();
	
			far_layers = new SpriteUV[2];
			for( int i=0 ; i<far_layers.Length ; ++i )
			{
				far_layers[i] = new SpriteUV(far_texture);

				far_layers[i].Quad.S = new Vector2(960,544);
	
				AddChild(far_layers[i]);
			}
			
			ground_layers = new SpriteUV[2];
			for( int i=0 ; i<ground_layers.Length ; ++i )
			{
				ground_layers[i] = new SpriteUV(ground_texture);

				ground_layers[i].Quad.S = new Vector2(960,544);
	
				AddChild(ground_layers[i]);
			}
			
			Scroll(0.0f);
		}
		
		public void Scroll( float position )
		{
			{
				var far_position = (position * 0.5f) % 960.0f;
				
				far_layers[0].Position = new Vector2(-far_position,0);
				far_layers[1].Position = new Vector2(-far_position+960,0);
			}

			{
				var ground_position = position % 960.0f;
				
				ground_layers[0].Position = new Vector2(-ground_position,0);
				ground_layers[1].Position = new Vector2(-ground_position+960,0);
			}
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
		
		//const float ground_level = 100.0f;
		
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
				if( Position.Y > GameScreen.GetHightInScreenSpace(Position.X) )
				{
					flying = true;
				}
				else
				{
					flying = false;

					speed.Y = 0.0f;
					Position = new Vector2( Position.X, GameScreen.GetHightInScreenSpace(Position.X) );
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

				// 敵とプレイヤの衝突
				{
					Func<SpriteList,float,bool> HitTestEnemyPlayer = (enemyList,distance2) =>
					{
						for( int enemy_index=0 ; enemy_index<enemyList.Children.Count; ++enemy_index )
						{
							var enemy = (Enemy)enemyList.Children[enemy_index];
							
							if( Vector2.DistanceSquared( enemy.Position, Position ) < distance2 )
							{
								var position = enemy.Position;
								GameScreen.CreateExplosion1(ref position);
								
								enemyList.RemoveChild(enemy,true);
								
								return true;
							}
						}
						
						return false;
					};
					
					bool hit = false;
					if(!hit){ hit = HitTestEnemyPlayer(GameScreen.enemy1List,GameScreen.collision_distance_enemy1_player2); }
					if(!hit){ hit = HitTestEnemyPlayer(GameScreen.enemy2List,GameScreen.collision_distance_enemy2_player2); }
					if(hit)
					{
						var position = Position;
						GameScreen.CreateExplosion1(ref position);
						
						Parent.RemoveChild(this,true);
						
						return;
					}
				}
			});
		}
		
		// 弾丸発射
		public void FireBullet( ref Vector2 direction )
		{
			Console.WriteLine("Fire!" + GameScreen.bulletList.Children.Count);
			
			var position = Position + direction * 50;
			
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
	
	// 敵ベースクラス
	public class Enemy : SpriteUV
	{
		public Enemy()
			: base()
		{
		}
		
		public bool OutsideOfScreen()
		{
			return ( Position.X < -100
			      || Position.X > 960+100
			      || Position.Y < -100
			      || Position.Y > 544+100 );
		}
	}

	// 敵1
	public class Enemy1 : Enemy
	{
		static Vector2 speed = new Vector2(-5,0);
		
		public Enemy1( ref Vector2 position )
			: base()
		{
			Position = position;
			
			// make the texture 1:1 on screen
			Quad.S = GameScreen.enemy1_texture.TextureSizef;
	
			// center the sprite around its own .Position 
			// (by default .Position is the lower left bit of the sprite)
			CenterSprite();

			// 毎フレーム処理		
			Schedule( (delta_time) => 
			{
				Position = Position + speed;
				
				if( OutsideOfScreen() )
				{
					Parent.RemoveChild(this,true);
				}
			});
		}
	}

	// 敵2
	public class Enemy2 : Enemy
	{
		static Vector2 speed = new Vector2(-3,0);
		
		Vector2 basePosition;
		float r = 0.0f;
		const float waveScale = 50.0f;
		const float waveSpeed = 3.0f;
		
		public Enemy2( ref Vector2 position )
			: base()
		{
			basePosition = position;
			Position = basePosition + new Vector2( 0, FMath.Sin(r) * waveScale );
			
			// make the texture 1:1 on screen
			Quad.S = GameScreen.enemy1_texture.TextureSizef;
	
			// center the sprite around its own .Position 
			// (by default .Position is the lower left bit of the sprite)
			CenterSprite();

			// 毎フレーム処理		
			Schedule( (delta_time) => 
			{
				r += delta_time * waveSpeed;
				
				basePosition += speed;
				Position = basePosition + new Vector2( 0, FMath.Sin(r) * waveScale );
				
				if( OutsideOfScreen() )
				{
					Parent.RemoveChild(this,true);
				}
			});
		}
	}

}

