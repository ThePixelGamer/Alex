using System.Linq;
using Alex.Blocks;
using Alex.Items;
using Alex.Worlds.Multiplayer.Bedrock;
using Alex.Worlds.Singleplayer;
using Newtonsoft.Json;
using NLog;

namespace Alex.Utils
{
	public static class ItemsExtensions
	{
		private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
		
		private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() {ReferenceLoopHandling = ReferenceLoopHandling.Ignore};

		public static Item ToAlexItem(this MiNET.Items.Item item)
		{
			if (item == null)
				return new ItemAir();

			Item result = null;


			var itemState = ChunkProcessor.Itemstates.FirstOrDefault(x => x.Id == item.Id);

			if (itemState == null)
				itemState = MiNET.Items.ItemFactory.Itemstates.FirstOrDefault(x => x.Id == item.Id);

			if (itemState != null)
			{
				if (ItemFactory.TryGetItem(itemState.Name, out result))
				{
					//	Log.Info($"{item.Id} = {JsonConvert.SerializeObject(itemState, SerializerSettings)} || {JsonConvert.SerializeObject(result, SerializerSettings)}");
				}
			}

			if (result == null && item.Id < 256 && item.Id >= 0) //Block
			{
				var id = item.Id;
				var meta = (byte) item.Metadata;

				var reverseMap = MiNET.Worlds.AnvilWorldProvider.Convert.FirstOrDefault(map => map.Value.Item1 == id);

				if (reverseMap.Value != null)
				{
					id = (byte) reverseMap.Key;
				}

				var res = BlockFactory.GetBlockStateID(id, meta);

				if (AnvilWorldProvider.BlockStateMapper.TryGetValue(res, out var res2))
				{
					var t = BlockFactory.GetBlockState(res2);

					ItemFactory.TryGetItem(t.Name, out result);
				}
			}

			if (result != null)
			{
				result.StackID = item.UniqueId;
				result.Meta = item.Metadata;
				result.Count = item.Count;
				result.Nbt = item.ExtraData;
				result.Id = item.Id;

				return result;
			}

			return new ItemAir() {Count = 0};
		}
	}
}