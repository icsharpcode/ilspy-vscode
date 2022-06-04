namespace ILSpy.Backend.Decompiler;

using ICSharpCode.ILSpyX;
using System;
using System.Xml.Linq;

public class SettingsProvider : ISettingsProvider
{
    private XElement root;

    public SettingsProvider()
    {
        root = new XElement("Root");
    }

    public XElement this[XName section] {
        get {
            return root.Element(section) ?? new XElement(section);
        }
    }

    public ISettingsProvider Load()
    {
        // No-op
        return this;
    }

    public void Update(Action<XElement> action)
    {
        action(root);
    }
}

