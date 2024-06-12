using System.Linq;
using System.Xml;
using System;

namespace NetshWfpViewer.Utilities
{
    internal static class WfpXmlFilter
    {
        public static void ApplyFilterToItem(XmlDocument document, string xpathSelector, string userFilterText, bool anyWord, bool removeWhenFound)
        {
            if (string.IsNullOrEmpty(userFilterText))
            {
                return;
            }

            var items = document.SelectNodes(xpathSelector);
            if (items == null)
            {
                return;
            }

            bool XmlContains(XmlElement element, string text)
            {
                return element.InnerXml.IndexOf(text, StringComparison.OrdinalIgnoreCase) > -1;
            }

            foreach (var i in items)
            {
                var item = (XmlElement)i;
                if (item == null)
                {
                    continue;
                }

                if (anyWord)
                {
                    string[] words =
                        userFilterText
                            .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim()).ToArray();

                    bool foundAnyWord = words.Any(word => XmlContains(item, word));

                    if (removeWhenFound)
                    {
                        if (foundAnyWord)
                        {
                            item.ParentNode?.RemoveChild(item);
                        }
                    }
                    else
                    {
                        if (!foundAnyWord)
                        {
                            item.ParentNode?.RemoveChild(item);
                        }
                    }
                }
                else
                {
                    if (removeWhenFound)
                    {
                        if (XmlContains(item, userFilterText))
                        {
                            item.ParentNode?.RemoveChild(item);
                        }
                    }
                    else
                    {
                        if (!XmlContains(item, userFilterText))
                        {
                            item.ParentNode?.RemoveChild(item);
                        }
                    }
                }
            }
        }
    }
}
