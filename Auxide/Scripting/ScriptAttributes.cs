using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class InfoAttribute : Attribute
{
    public string Name;
    public string Author;
    public string Version;

    public InfoAttribute(string name, string author, string version)
    {
        Name = name;
        Author = author;
        Version = version;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class DescriptionAttribute : Attribute
{
    public string Description;

    public DescriptionAttribute(string description)
    {
        Description = description;
    }
}
