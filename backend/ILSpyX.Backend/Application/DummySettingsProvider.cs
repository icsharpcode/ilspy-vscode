namespace ILSpyX.Backend.Application;

using ICSharpCode.ILSpyX.Settings;
using System;
using System.Xml.Linq;

public class DummySettingsProvider : ISettingsProvider
{
    private XElement root;

    public DummySettingsProvider()
    {
        root = new XElement("Root");
    }

    public XElement this[XName section] {
        get {
            return root.Element(section) ?? new XElement(section);
        }
    }

    public void Update(Action<XElement> action)
    {
        action(root);
    }

    public void SaveSettings(XElement section)
    {
        // No-op
    }
}

