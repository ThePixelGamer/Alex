﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Alex.API;
using Alex.API.Blocks;
using Alex.API.Graphics;
using Alex.API.Resources;
using Alex.API.Utils;
using Alex.API.Utils.Noise;
using Alex.API.Utils.Vectors;
using Alex.API.World;
using Alex.Blocks.Minecraft;
using Alex.Blocks.State;
using Alex.ResourcePackLib.Json;
using Alex.ResourcePackLib.Json.BlockStates;
using Alex.ResourcePackLib.Json.Models;
using Alex.ResourcePackLib.Json.Models.Blocks;
using Alex.Utils;
using Alex.Worlds;
using Alex.Worlds.Abstraction;
using Alex.Worlds.Chunks;
using Alex.Worlds.Singleplayer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using NLog;
using Matrix = System.Drawing.Drawing2D.Matrix;

namespace Alex.Graphics.Models.Blocks
{
	public class BlockStateModelWrapper
	{
		public BlockStateModel       BlockStateModel { get; }
		public ResourcePackModelBase BlockModel      { get; }

		public BlockStateModelWrapper(BlockStateModel model, ResourcePackModelBase blockModel)
		{
			BlockStateModel = model;
			BlockModel = blockModel;
		}
	}
	public class ResourcePackBlockModel : BlockModel
	{
		private static readonly Logger        Log = LogManager.GetCurrentClassLogger(typeof(SPWorldProvider));
		private static          SimplexPerlin NoiseGenerator { get; } = new SimplexPerlin(1337);

		static ResourcePackBlockModel()
		{
			
		}
		
		public static           bool       SmoothLighting { get; set; } = true;
		
		//private BlockStateModelWrapper[] Models    { get; set; }
		//private BlockStateResource BlockStateResource { get; }
		private ResourceManager    Resources          { get; }
		
		//private  BoundingBox[] Boxes         { get; set; }
		//private bool          UseRandomizer { get; set; }
		//private int           WeightSum     { get; }
		public ResourcePackBlockModel(ResourceManager resources, BlockStateResource blockStateResource)
		{
			Resources = resources;

			//Models = models.Where(x => x != null).ToArray();
			
			//Models = models;
		//	UseRandomizer = useRandomizer;
			//WeightSum = Models.Sum(x => x.BlockStateModel.Weight);
			
			//Boxes = CalculateBoundingBoxes(Models);
			/*
			Boxes = new BoundingBox[] {new BoundingBox(new Vector3(0f), new Vector3(1f))};
			for (int i = 0; i < Boxes.Length; i++)
			{
				var box        = Boxes[i];
				var dimensions = box.GetDimensions();

				if (dimensions.X < 0.015f)
				{
					var diff = (0.015f - dimensions.X) / 2f;
					box.Min.X -= diff;
					box.Max.X += diff;
					//box.Inflate(new Vector3(0.015f - dimensions.X, 0f, 0f));
				}
				
				if (dimensions.Y < 0.015f)
				{
					var diff = (0.015f - dimensions.Y) / 2f;
					box.Min.Y -= diff;
					box.Max.Y += diff;
					//box.Inflate(new Vector3(0f, 0.015f - dimensions.Y, 0f));
				}
				
				if (dimensions.Z < 0.015f)
				{
					var diff = (0.015f - dimensions.Z) / 2f;
					box.Min.Z -= diff;
					box.Max.Z += diff;
					//box.Inflate(new Vector3(0f, 0f, 0.015f - dimensions.Z));
				}

				Boxes[i] = box;
			}
			*/
		}
		
		/// <inheritdoc />
		public override IEnumerable<BoundingBox> GetBoundingBoxes(BlockState blockState, Vector3 blockPos)
		{
			if (blockState.BoundingBoxes == null)
			{
				List<BoundingBox> boxes = new List<BoundingBox>();
				foreach (var model in GetAppliedModels(blockState))
				{
					boxes.AddRange(GenerateBoundingBoxes(model.BlockStateModel, model.BlockModel));
				}

				blockState.BoundingBoxes = boxes.ToArray();
			}


			foreach (var box in blockState.BoundingBoxes)
			{
				var x = box;
				var dimensions = x.GetDimensions();

				if (dimensions.X < 0.015f)
				{
					var diff = (0.015f - dimensions.X) / 2f;
					x.Min.X -= diff;
					x.Max.X += diff;
					//box.Inflate(new Vector3(0.015f - dimensions.X, 0f, 0f));
				}

				if (dimensions.Y < 0.015f)
				{
					var diff = (0.015f - dimensions.Y) / 2f;
					x.Min.Y -= diff;
					x.Max.Y += diff;
					//box.Inflate(new Vector3(0f, 0.015f - dimensions.Y, 0f));
				}

				if (dimensions.Z < 0.015f)
				{
					var diff = (0.015f - dimensions.Z) / 2f;
					x.Min.Z -= diff;
					x.Max.Z += diff;
					//box.Inflate(new Vector3(0f, 0f, 0.015f - dimensions.Z));
				}

				yield return x.OffsetBy(blockPos);
				//yield return box;
			}
		}

		protected virtual bool ShouldRenderFace(IBlockAccess world, BlockFace face, BlockCoordinates position, Block me)
		{
			if (world == null) return true;
			
			if (position.Y >= 256) return true;

			if (face == BlockFace.None)
				return true;
				
			var pos = position + face.GetBlockCoordinates();

			var cX = (int)pos.X & 0xf;
			var cZ = (int)pos.Z & 0xf;

			if (cX < 0 || cX > 16)
				return false;

			if (cZ < 0 || cZ > 16)
				return false;
			
			var theBlock = world.GetBlockState(pos).Block;

			if (!theBlock.Renderable)
				return true;
			
			return me.ShouldRenderFace(face, theBlock);
		}


		private IEnumerable<BoundingBox> GenerateBoundingBoxes(BlockStateModel stateModel, ResourcePackLib.Json.Models.ResourcePackModelBase model)
		{
			//float facesMinX = float.MaxValue, facesMinY = float.MaxValue, facesMinZ = float.MaxValue;
		//	float facesMaxX = float.MinValue, facesMaxY = float.MinValue, facesMaxZ = float.MinValue;

			//List<BoundingBox> boxes = new List<BoundingBox>();
			for (var index = 0; index < model.Elements.Length; index++)
			{
				var eMinX   = float.MaxValue;
				var eMinY   = float.MaxValue;
				var eMinZ   = float.MaxValue;
				
				var eMaxX = float.MinValue;
				var eMaxY = float.MinValue;
				var eMaxZ = float.MinValue;
				
				var element = model.Elements[index];
				element.To *= Scale;
				element.From *= Scale;

				foreach (var face in element.Faces)
				{
					var facing   = face.Key;

					if (stateModel.X > 0f)
					{
						var offset = stateModel.X / 90;
						facing = RotateDirection(facing, offset, FACE_ROTATION_X, INVALID_FACE_ROTATION_X);
					}

					if (stateModel.Y > 0f)
					{
						var offset = stateModel.Y / 90;
						facing = RotateDirection(facing, offset, FACE_ROTATION, INVALID_FACE_ROTATION);
					}
					
					var verts = GetFaceVertices(face.Key, element.From, element.To, new BlockTextureData());
					verts = ProcessVertices(verts, stateModel, element, null, facing, face.Value);
					
					float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
					float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

					for (int i = 0; i < verts.Length; i++)
					{
						var v = verts[i];

						if (v.Position.X < minX)
						{
							minX = v.Position.X;
						}
						else if (v.Position.X > maxX)
						{
							maxX = v.Position.X;
						}

						if (v.Position.Y < minY)
						{
							minY = v.Position.Y;
						}
						else if (v.Position.Y > maxY)
						{
							maxY = v.Position.Y;
						}

						if (v.Position.Z < minZ)
						{
							minZ = v.Position.Z;
						}
						else if (v.Position.Z > maxZ)
						{
							maxZ = v.Position.Z;
						}
						
						//
						
						if (v.Position.X < eMinX)
						{
							eMinX = v.Position.X;
						}
						else if (v.Position.X > eMaxX)
						{
							eMaxX = v.Position.X;
						}

						if (v.Position.Y < eMinY)
						{
							eMinY = v.Position.Y;
						}
						else if (v.Position.Y > eMaxY)
						{
							eMaxY = v.Position.Y;
						}

						if (v.Position.Z < eMinZ)
						{
							eMinZ = v.Position.Z;
						}
						else if (v.Position.Z > eMaxZ)
						{
							eMaxZ = v.Position.Z;
						}

						verts[i] = v;
					}

					/*if (minX < facesMinX)
					{
						facesMinX = minX;
					}
					else if (maxX >facesMaxX)
					{
						facesMaxX = maxX;
					}

					if (minY < facesMinY)
					{
						facesMinY = minY;
					}
					else if (maxY > facesMaxY)
					{
						facesMaxY = maxY;
					}

					if (minZ < facesMinZ)
					{
						facesMinZ = minZ;
					}
					else if (maxZ > facesMaxZ)
					{
						facesMaxZ = maxZ;
					}*/
				}

				yield return new BoundingBox(new Vector3(eMinX, eMinY, eMinZ), new Vector3(eMaxX, eMaxY, eMaxZ));
			}
			//return boxes.ToArray();
		}

		private Vector3 FixRotation(
			Vector3 v,
			ModelElement element)
		{
			if (element.Rotation.Axis != Axis.Undefined)
			{
				var r      = element.Rotation;
				var angle  = (float) (r.Angle * (Math.PI / 180f));
				angle = (element.Rotation.Axis == Axis.Z) ? angle : -angle;
					
				var ci     = 1.0f / MathF.Cos(angle);
				
				var origin = r.Origin;
							
				var c = MathF.Cos(angle);
				var s = MathF.Sin(angle);

				v.X -= (origin.X / 16.0f);
				v.Y -= (origin.Y / 16.0f);
				v.Z -= (origin.Z / 16.0f);
				
				switch (r.Axis)
				{
					case Axis.Y:
					{
						var x = v.X;
						var z = v.Z;

						v.X = (x * c - z * s);
						v.Z = (z * c + x * s);
						
						if (r.Rescale) {
							v.X *= ci;
							v.Z *= ci;
						}
					}
						break;

					case Axis.X:
					{
						var x = v.Z ;
						var z = v.Y;

						v.Z = (x * c - z * s);
						v.Y =  (z * c + x * s);

						if (r.Rescale)
						{
							v.Z *= ci;
							v.Y *= ci;
						}
					}
						break;

					case Axis.Z:
					{
						var x = v.X;
						var z = v.Y;

						v.X = (x * c - z * s);
						v.Y = (z * c + x * s);

						if (r.Rescale)
						{
							v.X *= ci;
							v.Y *= ci;
						}
					}
						break;
				}
				
				v.X += (origin.X / 16.0f);
				v.Y += (origin.Y / 16.0f);
				v.Z += (origin.Z / 16.0f);
			}

			return v;
		}

		private BlockShaderVertex[] ProcessVertices(BlockShaderVertex[] vertices, 
			BlockStateModel bsModel,
			ModelElement element, 
			BlockTextureData? uvMap, 
			BlockFace blockFace, 
			ModelElementFace face)
		{
			for (int i = 0; i < vertices.Length; i++)
			{
				var v = vertices[i];

				var textureCoordinates = v.TexCoords;
				
				v.Position /= 16f;
				v.Position = FixRotation(v.Position, element);

				if (bsModel.X > 0)
				{
					var rotX = bsModel.X * (MathHelper.Pi / 180f);
					var c    = MathF.Cos(rotX);
					var s    = MathF.Sin(rotX);
					var z    = v.Position.Z - 0.5f;
					var y    = v.Position.Y - 0.5f;

					v.Position.Z = 0.5f + (z * c - y * s);
					v.Position.Y = 0.5f + (y * c + z * s);
				}

				if (bsModel.Y > 0)
				{
					var rotY = bsModel.Y * (MathHelper.Pi / 180f);
					var c    = MathF.Cos(rotY);
					var s    = MathF.Sin(rotY);
					var x    = v.Position.X - 0.5f;
					var z    = v.Position.Z - 0.5f;

					v.Position.X = 0.5f + (x * c - z * s);
					v.Position.Z = 0.5f + (z * c + x * s);
				}

				if (uvMap.HasValue)
				{
					var tw = uvMap.Value.TextureInfo.Width;
					var th = uvMap.Value.TextureInfo.Height;

					var rot = face.Rotation;

					if (rot > 0)
					{
						var rotY = rot * (MathHelper.Pi / 180f);
						var c    = MathF.Cos(rotY);
						var s    = MathF.Sin(rotY);
						
						var x    = textureCoordinates.X - 8f * tw;
						var y    = textureCoordinates.Y - 8f * th;

						textureCoordinates.X = 8f * tw + (x * c - y * s);
						textureCoordinates.Y = 8f * th + (y * c + x * s);
					}
					
					if (bsModel.Uvlock)
					{
						if (bsModel.Y > 0 && (blockFace == BlockFace.Up || blockFace == BlockFace.Down))
						{
							var rotY = bsModel.Y * (MathHelper.Pi / 180f);
							var c    = MathF.Cos(rotY);
							var s    = MathF.Sin(rotY);
							var x    = textureCoordinates.X - 8f * tw;
							var y    = textureCoordinates.Y - 8f * th;

							textureCoordinates.X = 8f * tw + (x * c - y * s);
							textureCoordinates.Y = 8f * th + (y * c + x * s);
						}

						if (bsModel.X > 0 && (blockFace != BlockFace.Up && blockFace != BlockFace.Down))
						{
							var rotX = bsModel.X * (MathHelper.Pi / 180f);
							var c    = MathF.Cos(rotX);
							var s    = MathF.Sin(rotX);
							var x    = textureCoordinates.X - 8f * tw;
							var y    = textureCoordinates.Y - 8f * th;

							textureCoordinates.X = 8f * tw + (x * c - y * s);
							textureCoordinates.Y = 8f * th + (y * c + x * s);
						}
					}
					
					
					textureCoordinates += uvMap.Value.TextureInfo.Position;
				//	textureCoordinates *= (Vector2.One / uvMap.Value.TextureInfo.AtlasSize);
				}

				v.TexCoords = textureCoordinates;
				v.Face = blockFace;
				vertices[i] = v;
			}

			return vertices;
		}

		private void CalculateModel(IBlockAccess world,
			BlockCoordinates blockCoordinates,
			ChunkData chunkBuilder,
			Vector3 position,
			Block baseBlock,
			BlockStateModel blockStateModel,
			ResourcePackModelBase model,
			Biome biome)
		{
			var positionOffset = baseBlock.GetOffset(NoiseGenerator, position);
			//bsModel.Y = Math.Abs(180 - bsModel.Y);

			//if (Resources.BlockModelRegistry.TryGet(blockStateModel.ModelName, out var registryEntry))
			{
				//var model = registryEntry.Value;

				var baseColor = baseBlock.BlockMaterial.TintColor;

				for (var index = 0; index < model.Elements.Length; index++)
				{
					var element = model.Elements[index];
					element.To *= Scale;

					element.From *= Scale;

					//var faces = element.Faces.ToArray();
					foreach (var face in element.Faces)
					{
						var facing   = face.Key;
						//var cullFace = face.Value.CullFace ?? face.Key;

						if (blockStateModel.X > 0f)
						{
							var offset = blockStateModel.X / 90;
							//cullFace = RotateDirection(cullFace, offset, FACE_ROTATION_X, INVALID_FACE_ROTATION_X);
							facing = RotateDirection(facing, offset, FACE_ROTATION_X, INVALID_FACE_ROTATION_X);
						}

						if (blockStateModel.Y > 0f)
						{
							var offset = blockStateModel.Y / 90;
							//cullFace = RotateDirection(cullFace, offset, FACE_ROTATION, INVALID_FACE_ROTATION);
							facing = RotateDirection(facing, offset, FACE_ROTATION, INVALID_FACE_ROTATION);
						}

						float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
						float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

						
						
						if (!ShouldRenderFace(world, facing, position, baseBlock))
							continue;

						var   uv = face.Value.UV;
						float x1 = 0, x2 = 0, y1 = 0, y2 = 0;

						if (uv == null)
						{
							switch (face.Key)
							{
								case BlockFace.North:
								case BlockFace.South:
									x1 = element.From.X;
									x2 = element.To.X;
									y1 = 16f - element.To.Y;
									y2 = 16f - element.From.Y;

									break;

								case BlockFace.West:
								case BlockFace.East:
									x1 = element.From.Z;
									x2 = element.To.Z;
									y1 = 16f - element.To.Y;
									y2 = 16f - element.From.Y;

									break;

								case BlockFace.Down:
								case BlockFace.Up:
									x1 = element.From.X;
									x2 = element.To.X;
									y1 = 16f - element.To.Z;
									y2 = 16f - element.From.Z;

									break;
							}
						}
						else
						{
							x1 = uv.X1;
							x2 = uv.X2;
							y1 = uv.Y1;
							y2 = uv.Y2;
						}

						var faceColor = baseColor;

						bool hasTint = (face.Value.TintIndex.HasValue && face.Value.TintIndex == 0);

						if (hasTint)
						{
							switch (baseBlock.BlockMaterial.TintType)
							{
								case TintType.Default:
									faceColor = Color.White;

									break;

								case TintType.Color:
									faceColor = baseBlock.BlockMaterial.TintColor;

									break;

								case TintType.Grass:
									if (SmoothLighting)
									{
										var bx = (int) position.X;
										var y  = (int) position.Y;
										var bz = (int) position.Z;

										faceColor = CombineColors(
											GetGrassBiomeColor(world, bx, y, bz),
											GetGrassBiomeColor(world, bx - 1, y, bz),
											GetGrassBiomeColor(world, bx, y, bz - 1),
											GetGrassBiomeColor(world, bx + 1, y, bz),
											GetGrassBiomeColor(world, bx, y, bz + 1),
											GetGrassBiomeColor(world, bx + 1, y, bz - 1));
									}
									else
									{

										faceColor = Resources.GetGrassColor(
											biome.Temperature, biome.Downfall, (int) position.Y);
									}

									break;

								case TintType.Foliage:
									if (face.Value.TintIndex.HasValue && face.Value.TintIndex == 0)
									{
										faceColor = Resources.GetFoliageColor(
											biome.Temperature, biome.Downfall, (int) position.Y);
									}

									break;

								default:
									throw new ArgumentOutOfRangeException();
							}
						}

						faceColor = AdjustColor(faceColor, facing, element.Shade);

						BlockTextureData uvMap = GetTextureUVMap(
								Resources, face.Value.Texture, x1, x2, y1, y2, face.Value.Rotation, faceColor, null);
						
						var vertices = GetFaceVertices(face.Key, element.From, element.To, uvMap);

						vertices = ProcessVertices(vertices, blockStateModel, element, uvMap, facing, face.Value);

						RenderStage targetState = RenderStage.Opaque;

						if (baseBlock.BlockMaterial.IsLiquid)
						{
							targetState = RenderStage.Liquid;
						}
						else if (uvMap.IsAnimated)
						{
							targetState = RenderStage.Animated;
						}
						else if (baseBlock.Transparent)
						{
							if (baseBlock.BlockMaterial.IsOpaque)
							{
								if (!Block.FancyGraphics && baseBlock.IsFullCube)
								{
									targetState = RenderStage.Opaque;
								}
								else
								{
									targetState = RenderStage.Transparent;
								}
							}
							else
							{
								targetState = RenderStage.Translucent;
							}
						}
						else if (!baseBlock.IsFullCube)
						{
							targetState = RenderStage.Opaque;
						}

						for (int i = 0; i < vertices.Length; i++)
						{
							var vertex = vertices[i];

							BlockModel.GetLight(
								world, vertex.Position + position + vertex.Face.GetVector3(), out var blockLight,
								out var skyLight, true);

							chunkBuilder.AddVertex(
								blockCoordinates, vertex.Position + position + positionOffset, vertex.Face,
								new Vector4(
									vertex.TexCoords.X, vertex.TexCoords.Y,
									uvMap.TextureInfo.Animated ? uvMap.TextureInfo.Width : 1,
									uvMap.TextureInfo.Animated ? uvMap.TextureInfo.Height: 1), vertex.Color, blockLight, skyLight,
								targetState);
						}
					}
				}
			}
		}

		private Color GetGrassBiomeColor(IBlockAccess access, int x, int y, int z)
		{
			var biome = access.GetBiome(new BlockCoordinates(x, y, z));
			return Resources.GetGrassColor(
				biome.Temperature, biome.Downfall, y);
		}

		private IEnumerable<BlockStateModelWrapper> GetAppliedModels(BlockState baseBlock)
		{
			if (baseBlock.ModelData == null)
				yield break;
			
			if (baseBlock.VariantMapper.IsMultiPart)
			{
				foreach (var model in baseBlock.ModelData)
				{
					if (Resources.BlockModelRegistry.TryGet(model.ModelName, out var registryEntry))
					{
						yield return new BlockStateModelWrapper(model, registryEntry.Value);
					}
				}

				yield break;

			}
			
			//	if (UseRandomizer)
			{
				BlockStateModel selectedModel = null;

				if (baseBlock.ModelData.Count > 1)
				{
					var weightSum = baseBlock.ModelData.Sum(x => x.Weight);

					var rnd = weightSum;

					//for (var index = 0; index < Models.Length; index++)
					foreach (var model in baseBlock.ModelData)
					{
						//	var model = Models[index];
						rnd -= model.Weight;

						if (rnd < 0)
						{
							selectedModel = model;

							break;
						}
					}
				}
				else
				{
					selectedModel = baseBlock.ModelData.FirstOrDefault();
				}

				if (selectedModel == null)
					yield break;

				if (Resources.BlockModelRegistry.TryGet(selectedModel.ModelName, out var registryEntry))
				{
					yield return new BlockStateModelWrapper(selectedModel, registryEntry.Value);
				}

				//	CalculateModel(world, blockCoordinates, chunkBuilder, position, baseBlock, selectedModel.BlockStateModel, selectedModel.BlockModel, biome);
			}
		}
		
		public override void GetVertices(IBlockAccess world,
			ChunkData chunkBuilder,
			BlockCoordinates blockCoordinates,
			Vector3 position,
			BlockState baseBlock)
		{
			var biome = world == null ? BiomeUtils.GetBiomeById(0) : world.GetBiome(position);
			
			
			if (baseBlock.ModelData == null)
				return;
			
			if (baseBlock.VariantMapper.IsMultiPart)
			{
				foreach (var model in baseBlock.ModelData)
				{
					if (Resources.BlockModelRegistry.TryGet(model.ModelName, out var registryEntry))
					{
						CalculateModel(
							world, blockCoordinates, chunkBuilder, position, baseBlock.Block, model,
							registryEntry.Value, biome);
					}
				}

				return;
			}
			
		//	if (UseRandomizer)
			{
				BlockStateModel selectedModel = null;

				if (baseBlock.ModelData.Count > 1)
				{
					var weightSum = baseBlock.ModelData.Sum(x => x.Weight);

					var rnd = ((baseBlock.ModelData.Count == 1) ? 1f : MathF.Abs(
						NoiseGenerator.GetValue(position.X * position.Y, position.Z * position.X))) * weightSum;

					//for (var index = 0; index < Models.Length; index++)
					foreach (var model in baseBlock.ModelData)
					{
						//	var model = Models[index];
						rnd -= model.Weight;

						if (rnd < 0)
						{
							selectedModel = model;

							break;
						}
					}
				}
				else
				{
					selectedModel = baseBlock.ModelData.FirstOrDefault();
				}

				if (selectedModel == null)
					return;

				if (Resources.BlockModelRegistry.TryGet(selectedModel.ModelName, out var registryEntry))
				{
					CalculateModel(
						world, blockCoordinates, chunkBuilder, position, baseBlock.Block, selectedModel,
						registryEntry.Value, biome);
				}

				//	CalculateModel(world, blockCoordinates, chunkBuilder, position, baseBlock, selectedModel.BlockStateModel, selectedModel.BlockModel, biome);
			}
			//else
			//{
			//for (var bsModelIndex = 0; bsModelIndex < Models.Length; bsModelIndex++)
			//{
			//var bsModel = Models[bsModelIndex];

			//	if (string.IsNullOrWhiteSpace(bsModel.ModelName)) continue;

			//}
			//}
		}
	}
}