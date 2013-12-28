using System;

using Sce.PlayStation.Core;
using Sce.PlayStation.Core.Environment;
using Sce.PlayStation.Core.Graphics;
using Sce.PlayStation.Core.Input;

using Sce.PlayStation.HighLevel.GameEngine2D;
using Sce.PlayStation.HighLevel.GameEngine2D.Base;

namespace shooting1
{
	public static class GameScreen
	{
		public static Scene CreateScene()
		{
			var scene = new Scene(){ Name = "Game" };

			// set the camera so that the part of the word we see on screen matches in screen coordinates
			scene.Camera.SetViewFromViewport();
	
			// create a new TextureInfo object, used by sprite primitives
			var texture_info = new TextureInfo( new Texture2D("/Application/textures/background.png", false ) );
	
			// create a new sprite
			var sprite = new SpriteUV() { TextureInfo = texture_info};
	
			// make the texture 1:1 on screen
			//sprite.Quad.S = texture_info.TextureSizef; 
			sprite.Quad.S = new Vector2(100,100);
	
			// center the sprite around its own .Position 
			// (by default .Position is the lower left bit of the sprite)
			sprite.CenterSprite();
	
			// put the sprite at the center of the screen
			sprite.Position = scene.Camera.CalcBounds().Center;
	
			// our scene only has 2 nodes: scene->sprite
			scene.AddChild( sprite );
			
			return scene;
		}
	}
}

