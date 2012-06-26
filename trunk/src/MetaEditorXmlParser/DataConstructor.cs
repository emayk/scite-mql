﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MetaEditorXmlParser.NodeType;

namespace MetaEditorXmlParser
{
  public class DataConstructor
  {
    public XElement StyleNode { get; private set; }
    public IList<BaseNode> PageNodes { get; private set; }
    public IList<string> Constants { get; private set; }
    private BaseNode Root { get; set; }

    public DataConstructor()
    {
      PageNodes = new List<BaseNode>();
      Constants = new List<string>();
        Root = new BaseNode(null);
    }

    public void Analyze()
    {
      var elements = from c in XElement.Load("metaeditor.xml").Elements() select c;
      foreach (var e in elements)
      {
          var b = ProcessNode(e);
          if (b != null)
              Root.Children.Add(b);
      }
    }

    public void ProduceChmMaterial(string directory)
    {
        string fileList = "";
        foreach (var n in PageNodes)
        {
            var fn = ScribeHtml(n, directory);
            fileList += fn + "\n";
        }

        ScibeHhp(fileList, directory);
        ScribeHhc(directory);
        ScribeHhk(directory);
    }

    private BaseNode ProcessNode(XElement element)
    {
      string name = element.Name.ToString();
      
      switch (name)
      {
        case "content":
        case "example":
        case "tag":
        case "caption":
        case "type":
        case "name":
          case "keywords":
          return null;

        case "styles":
          StyleNode = element;
          return null;
              
          case "parameter":
          element = XmlCompliantConverter.ParseNameValueToElement(element);
          return null;
      }
      element = XmlCompliantConverter.ParseNameValueToElement(element);
        var children = new List<BaseNode>();
      foreach (var e in element.Elements())
      {
          var n = ProcessNode(e);
          if (n != null) children.Add(n);
      }
        BaseNode node = BaseNode.GetNode(element);
      if (!string.IsNullOrEmpty(node.Key)) // it's a page
      {
          node.Children = children;
        PageNodes.Add(node);
      }

      if (node.Keywords != null)
        foreach (var s in node.Keywords)
          Constants.Add(s);

      return node;
    }

    private string ScribeHtml(BaseNode node, string directory)
    {
      if (!Directory.Exists(directory))
        Directory.CreateDirectory(directory);

        string fileName = node.Key + ".html";
      using (StreamWriter sw = new StreamWriter(Path.Combine(directory, fileName), false, Encoding.UTF8))
      {
        sw.WriteLine(@"<html><head><meta http-equiv=""Content-Type"" content=""text/html; charset=""utf-8"" /><title>" + node.Title + "</title>");
        sw.WriteLine("<style type='text/css'>");
        sw.WriteLine(StyleNode.InnerText());
        sw.WriteLine("</style></head><body>");
        sw.WriteLine(node.ToHtml());
        sw.WriteLine("</body></html>");
      }

        return fileName;
    }

    private void ScibeHhp(string fileList, string directory)
    {
        using (StreamWriter sw = new StreamWriter(Path.Combine(directory, "mql.hhp"), false, Encoding.UTF8))
        {
            sw.WriteLine(@"[OPTIONS]
Compatibility=1.1 Or later
Binary TOC=Yes
Default window=Main
Contents file=mql.hhc
Index file=mql.hhk
Display compile progress=Yes
Full-text search=Yes
Default topic=index.html

[WINDOWS]
Main=,""mql.hhc"",""mql.hhk"",,""index.html"",,,,,0x10063560,,0x70387E,,,,,,,,
");
            sw.WriteLine(fileList);
        }
    }

    private void ScribeHhc(string directory)
    {
        using (StreamWriter sw = new StreamWriter(Path.Combine(directory, "mql.hhc"), false, Encoding.UTF8))
        {
            sw.WriteLine(@"<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML//EN"">
<HTML>
<HEAD>
<meta name=""GENERATOR"" content=""Microsoft® HTML Help Workshop 4.1"">
<!-- Sitemap 1.0 -->
</HEAD><BODY>
<UL>");
            WriteNode(sw, Root.Children[0]);
            sw.WriteLine("</UL></BODY></HTML>");
        }
    }

    private void WriteNode(StreamWriter sw, BaseNode node)
    {
        sw.WriteLine(@"<LI><OBJECT type=""text/sitemap"">
<param name=""Name"" value=""" + node.Title.Replace("()", "").Replace("[]", "") + @""">
<param name=""Local"" value=""" + node.Key + @".html"">
</OBJECT>");
        if (node.Children.Count > 0)
        {
            sw.WriteLine("<UL>");
            foreach (var c in node.Children)
                WriteNode(sw, c);
            sw.WriteLine("</UL>");
        }
    }

    private void ScribeHhk(string directory)
    {
        var keywordsDict = new Dictionary<string, IList<BaseNode>>();
        foreach (var n in PageNodes)
        {
            if (n.Keywords != null)
            {
                foreach (var k in n.Keywords)
                {
                    if (!keywordsDict.ContainsKey(k))
                        keywordsDict[k] = new List<BaseNode>();
                    keywordsDict[k].Add(n);
                }
            }
            else
            {
                var name = n.Element.Name.ToString();
                switch (name)
                {
                    case "variable":
                    case "function":
                        int c = n.Title.IndexOf(' ');
                        var k = (n.Title.IndexOf(' ') > 0 ? n.Title.Substring(0, c) : n.Title).Replace("()", "").Replace("[]", "");
                        if (!keywordsDict.ContainsKey(k))
                            keywordsDict[k] = new List<BaseNode>();
                        if (!keywordsDict[k].Any(x => x == n))
                            keywordsDict[k].Add(n);
                        break;
                }
            }
        }

        using (StreamWriter sw = new StreamWriter(Path.Combine(directory, "mql.hhk"), false, Encoding.UTF8))
        {
            sw.WriteLine(@"<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML//EN"">
<HTML>
<HEAD>
<meta name=""GENERATOR"" content=""Microsoft® HTML Help Workshop 4.1"">
<!-- Sitemap 1.0 -->
</HEAD><BODY>
<UL>");
            foreach (var k in keywordsDict.Keys)
            {
                sw.WriteLine(@"<LI><OBJECT type=""text/sitemap"">
 <param name=""Name"" value=""" + k + @""">");
                foreach (var e in keywordsDict[k])
                    sw.WriteLine(@"<param name=""Local"" value=""" + e.Key + @".html"">");
                sw.WriteLine(@"</OBJECT>");
            }
            sw.WriteLine("</UL></BODY></HTML>");
        }
    }
  }
}
