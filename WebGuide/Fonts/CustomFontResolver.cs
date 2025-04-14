using PdfSharpCore.Fonts;
using System.IO;
using System.Reflection;

public class CustomFontResolver : IFontResolver
{
    private static readonly string _fontName = "DejaVuSans#";

    public static void Register()
    {
        GlobalFontSettings.FontResolver = new CustomFontResolver();
    }

    public string DefaultFontName => _fontName;

    public byte[] GetFont(string faceName)
    {
        var assembly = typeof(CustomFontResolver).GetTypeInfo().Assembly;
        var resourceName = "WebGuide.Fonts.DejaVuSans.ttf";

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Шрифт '{resourceName}' не знайдено у вбудованих ресурсах.");

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(_fontName);
    }
}
