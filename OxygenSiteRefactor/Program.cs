using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Web;

Console.WriteLine("Enter absolute path of the oxygen site folder: ");
var oxygenPath = Console.ReadLine();
var rootDir = oxygenPath.Replace('\\', '/').Split('/').Last();

Console.WriteLine("Enter root folder of the oxygen site in wwwroot: ");
var wwwrootDir = Console.ReadLine();

Console.WriteLine("Enter 'oxygen-webhelp\\app' folder absolute path: ");
var appPath = Console.ReadLine();


string[] allfiles = Directory.GetFiles(oxygenPath, "*.html", SearchOption.AllDirectories);

foreach(string file in allfiles)
{
    var doc = new HtmlDocument();
    var html = File.ReadAllText(file);
    doc.LoadHtml(html);
    Console.WriteLine($"Strat Modify File:{file}");
    doc = ModifyA(doc, file, rootDir);
    doc = ModifyIMG(doc, wwwrootDir);
    doc = ModifyForm(doc);
    doc = ModifyCSSRef(doc, wwwrootDir);
    doc = ModifyJSRef(doc, wwwrootDir);
    doc.Save(file);
}

ModifyJSContent(Path.Combine(appPath, "search", "search.js"));

ModifyMenuContent(Path.Combine(appPath, "nav-links", "json"));

static HtmlDocument ModifyIMG(HtmlDocument doc, string wwwrootDir)
{

    var imgs = doc.DocumentNode.SelectNodes("//img");
    if (imgs == null)
        return doc;
    foreach (var link in imgs)
    {
        var srcValue = link.GetAttributeValue("src", string.Empty);

        var newSrc = $"{wwwrootDir}/{srcValue.Replace("../","").Replace("./", "")}";

        link.SetAttributeValue("src", newSrc);
    }
    return doc;
}

static HtmlDocument ModifyA(HtmlDocument doc, string path,string rootDir)
{
    var links = doc.DocumentNode.SelectNodes("//a");
    if (links == null)
        return doc;
    foreach (var link in links)
    {
        var hrefValue = link.GetAttributeValue("href", string.Empty);

        if (hrefValue.StartsWith('#'))
            continue;

        if (hrefValue.StartsWith("mailto://"))
            continue;

        if (hrefValue.Contains("index.html"))
        {
            link.SetAttributeValue("href", "/Document");
            continue;
        }

        var newLink = $"/Document?url={HttpUtility.UrlEncode(GetOrignalPath(path ,hrefValue, rootDir))}";

        link.SetAttributeValue("href", newLink);
    }
    return doc;
}

static HtmlDocument ModifyForm(HtmlDocument doc)
{
    var forms = doc.DocumentNode.SelectNodes("//form");
    if (forms == null)
        return doc;
    foreach (var link in forms)
    {
        var newLink = "/Document?url=search.html";

        link.SetAttributeValue("action", newLink);
    }
    return doc;
}

static HtmlDocument ModifyCSSRef(HtmlDocument doc, string wwwrootDir)
{
    var cssRefs = doc.DocumentNode.SelectNodes("//link");
    if (cssRefs == null)
        return doc;
    foreach (var link in cssRefs)
    {
        var srcValue = link.GetAttributeValue("href", string.Empty);

        var newSrc = $"{wwwrootDir}/{srcValue.Replace("../", "").Replace("./", "")}";

        link.SetAttributeValue("href", newSrc);
    }
    return doc;
}

static HtmlDocument ModifyJSRef(HtmlDocument doc, string wwwrootDir)
{
    var scripts = doc.DocumentNode.SelectNodes("//script");
    if (scripts == null)
        return doc;
    foreach (var link in scripts)
    {
        var srcValue = link.GetAttributeValue("src", string.Empty);

        if (string.IsNullOrEmpty(srcValue))
            continue;

        var newSrc = $"{wwwrootDir}/{srcValue.Replace("../", "").Replace("./","")}";

        link.SetAttributeValue("src", newSrc);

        var data_main = link.GetAttributeValue("data-main",string.Empty);
        if (!string.IsNullOrEmpty(data_main))
        {
            var newData_main = $"{wwwrootDir}/{data_main.Replace("../", "").Replace("./", "")}";
            link.SetAttributeValue("data-main", newData_main);
        }
    }
    return doc;
}

static void ModifyJSContent(string searchjsPath)
{
    var js = File.ReadAllText(searchjsPath);
    js = js.Replace(
        "var tempPath = searchItem.relativePath;",
        "var tempPath = \"Document?url=%2F\" + encodeURIComponent(searchItem.relativePath);")
        .Replace(
        "tempPath += '?hl=' + encodeURIComponent(arrayString);",
        "tempPath += '&hl=' + encodeURIComponent(arrayString);"
        )
        .Replace(
        "href: item.relativePath,",
        "href: \"Document?url=%2F\" + encodeURIComponent(item.relativePath),")
        .Replace(
        "}).html(searchItem.relativePath);",
        "}).html(\"Document?url=%2F\" + encodeURIComponent(searchItem.relativePath));"
        );
    File.WriteAllText( searchjsPath, js );
}

static void ModifyMenuContent(string folderpath)
{
    string[] allfiles = Directory.GetFiles(folderpath, "*.js", SearchOption.AllDirectories);

    foreach (string file in allfiles)
    {
        var content = File.ReadAllText(file);
        content = content.Substring(7, content.Length - 9);
        dynamic json = JsonConvert.DeserializeObject(content);
        foreach (var item in json.topics)
        {
            if (item.href == null)
                continue;
            item.href = "Document?url=" + HttpUtility.UrlEncode("/" + Convert.ToString(item.href).Replace("\\/", "/"));
        }
        File.WriteAllText(file, "define(" + JsonConvert.SerializeObject(json) + ");");
    }
    
}

static string GetOrignalPath(string filePath, string relativePath,string rootDir)
{
    var absolute_path = Path.GetFullPath(
        Path.Combine(
            new FileInfo(filePath).Directory.ToString().Replace('\\', '/'),
            relativePath.Replace('\\', '/')
            )
        );
    return absolute_path.Substring(
        absolute_path.IndexOf(rootDir) + rootDir.Length, 
        absolute_path.Length - absolute_path.IndexOf(rootDir) - rootDir.Length )
        .Replace('\\', '/');
}