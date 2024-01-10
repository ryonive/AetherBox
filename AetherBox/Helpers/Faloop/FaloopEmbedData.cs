using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using AetherBox.Helpers.Faloop.Model.Embed;

namespace AetherBox.Helpers.Faloop;

public class FaloopEmbedData
{
	private readonly FaloopApiClient client;

	public List<ZoneLocationData> ZoneLocations { get; private set; } = new List<ZoneLocationData>();


	public List<MobData> Mobs { get; private set; } = new List<MobData>();


	public FaloopEmbedData(FaloopApiClient client)
	{
		this.client = client;
	}

	public async Task Initialize()
	{
		string content = await GetMainScript();
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
		foreach (JsonNode item in ExtractJsonNodes(content))
		{
			if (!(item is JsonArray { Count: >0 } array))
			{
				continue;
			}
			JsonNode jsonNode = array[0];
			JsonObject obj = jsonNode as JsonObject;
			if (obj != null)
			{
				if (new string[5] { "id", "key", "rank", "version", "zoneIds" }.All((string x) => obj.ContainsKey(x)))
				{
					Mobs = array.Deserialize<List<MobData>>(options);
				}
				if (new string[4] { "id", "zoneId", "type", "location" }.All((string x) => obj.ContainsKey(x)))
				{
					ZoneLocations = array.Deserialize<List<ZoneLocationData>>(options);
				}
			}
		}
	}

	private async Task<string> GetMainScript()
	{
		string html = await client.DownloadText(new Uri("https://faloop.app/"));
		string src = ((await new HtmlParser().ParseDocumentAsync(html)).QuerySelector("script[src^=\"main\"]") ?? throw new ApplicationException("Could not find main.js")).GetAttribute("src");
		if (string.IsNullOrWhiteSpace(src))
		{
			throw new ApplicationException("src attribute not found.");
		}
		UriBuilder uri = new UriBuilder("https", "faloop.app")
		{
			Path = src
		};
		return await client.DownloadText(uri.Uri);
	}

	private static IEnumerable<JsonNode> ExtractJsonNodes(string content)
	{
		Regex regex = new Regex("JSON\\.parse\\('(.+?)'\\)", RegexOptions.Multiline);
		foreach (Match item in from x in regex.Matches(content)
			where x.Success && x.Groups[1].Success
			select x)
		{
			string json = Regex.Unescape(item.Groups[1].Value);
			yield return JsonNode.Parse(json);
		}
	}
}
