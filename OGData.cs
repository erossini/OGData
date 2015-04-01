using System;
using System.Web;
using System.Web.UI;
using System.Collections.Generic;
using System.Linq;
using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
using BlogEngine.Core.Web.Extensions;
using System.Web.UI.HtmlControls;

/// <summary>
/// Insert in the head tag the og detail
/// </summary>
[Extension("Adds an open group tag to first image in your post for any social network identification. Social networks will show this image as a thumbnail when shared on their platform", "1.0", "<a href=\"http://www.puresourcecode.com\">Enrico Rossini</a>")]
public class OGData
{
    #region Constants and Fields

    /// <summary>
    /// The sync root.
    /// </summary>
    private static readonly object syncRoot = new object();

    /// <summary>
    /// The settings.
    /// </summary>
    private static Dictionary<Guid, ExtensionSettings> blogsSettings = new Dictionary<Guid, ExtensionSettings>();

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// static constructor to init the settings
    /// </summary>
    public OGData()
    {
        Post.Serving += AddOGImageThumbnail;
        BlogEngine.Core.Page.Serving += AddOGImageThumbnail;

        var s = Settings;
    }

    /// <summary>
    /// Gets or sets the settings.
    /// </summary>
    /// <value>The settings.</value>
    protected static ExtensionSettings Settings
    {
        get
        {
            Guid blogId = Blog.CurrentInstance.Id;

            if (!blogsSettings.ContainsKey(blogId))
            {
                lock (syncRoot)
                {
                    if (!blogsSettings.ContainsKey(blogId))
                    {
                        // create settings object. You need to pass exactly your
                        // extension class name (case sencitive)
                        var extensionSettings = new ExtensionSettings("OGData");

                        // -----------------------------------------------------
                        // 1. Simple
                        // -----------------------------------------------------
                        // settings.AddParameter("Code");
                        // settings.AddParameter("OpenTag");
                        // settings.AddParameter("CloseTag");
                        // -----------------------------------------------------
                        // 2. Some more options
                        // -----------------------------------------------------
                        // settings.AddParameter("Code");
                        // settings.AddParameter("OpenTag", "Open Tag");
                        // settings.AddParameter("CloseTag", "Close Tag");

                        //// describe specific rules applied to entering parameters. overrides default wording.
                        // -----------------------------------------------------
                        // 3. More options including import defaults
                        // -----------------------------------------------------

                        extensionSettings.AddParameter("OGDataTitle", "Add title", 30, false, true, ParameterType.String);
                        extensionSettings.AddParameter("OGDataImage", "Defaul path image", 100, false, true, ParameterType.String);

                        extensionSettings.AddValue("OGDataTitle", "");
                        extensionSettings.AddValue("OGDataImage", "");

                        // describe specific rules for entering parameters
                        extensionSettings.Help = @"You can choose the test to add to the title and the default image";

                        //extensionSettings.AddValues(new[] { "b", "strong", string.Empty });

                        // ------------------------------------------------------
                        ExtensionManager.ImportSettings(extensionSettings);
                        blogsSettings[blogId] = ExtensionManager.GetSettings("OGData");
                    }
                }
            }

            return blogsSettings[blogId];
        }
    }

    #endregion

    /// <summary>
    /// On Post Served event handler will post to social networks
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AddOGImageThumbnail(object sender, ServingEventArgs e)
    {
        var s = Settings;
        string title = string.IsNullOrEmpty(s.GetSingleValue("OGDataTitle")) ? "" : s.GetSingleValue("OGDataTitle");
        string image = string.IsNullOrEmpty(s.GetSingleValue("OGDataImage")) ? "" : s.GetSingleValue("OGDataImage");

        if (e.Location == ServingLocation.SinglePage)
        {
            BlogEngine.Core.Page p = (BlogEngine.Core.Page)sender;
            AddMetaData("og:title", p.Title + title);
            AddMetaData("og:type", "website");
            AddMetaData("og:url", p.AbsoluteLink.AbsoluteUri.ToLower());
            AddMetaData("og:description", p.Description);
            string imgPath = string.Empty;
            HttpContext context = HttpContext.Current;

            if (context.CurrentHandler is System.Web.UI.Page)
            {
                System.Web.UI.Page page = (System.Web.UI.Page)context.CurrentHandler;
                imgPath = getImage(true, p.Content);

                if (!string.IsNullOrEmpty(imgPath))
                {
                    if (!imgPath.ToLower().Contains("http://"))
                    {
                        imgPath = context.Request.Url.Scheme + "://" + context.Request.Url.Authority + imgPath;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(image))
                    {
                        imgPath = image;
                    }
                    else
                    {
                        imgPath = context.Request.Url.Scheme + "://" + context.Request.Url.Authority;
                    }
                }

                AddHeader(page, imgPath);
            }
        }

        if (e.Location == ServingLocation.SinglePost)
        {
            Post p = (Post)sender;
            AddMetaData("og:title", p.Title + title);
            AddMetaData("og:description", p.Description);
            AddMetaData("og:url", p.AbsoluteLink.AbsoluteUri.ToLower());
            AddMetaData("article:author", p.Author);

            string imgPath = string.Empty;
            HttpContext context = HttpContext.Current;
            if (context.CurrentHandler is System.Web.UI.Page)
            {
                System.Web.UI.Page page = (System.Web.UI.Page)context.CurrentHandler;
                imgPath = getImage(true, p.Content);

                if (!string.IsNullOrEmpty(imgPath))
                {
                    if (!imgPath.ToLower().Contains("http://"))
                    {
                        imgPath = context.Request.Url.Scheme + "://" + context.Request.Url.Authority + imgPath;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(image))
                    {
                        imgPath = image;
                    }
                    else
                    {
                        imgPath = context.Request.Url.Scheme + "://" + context.Request.Url.Authority;
                    }
                }

                AddHeader(page, imgPath);
            }
        }
    }

    private void AddHeader(System.Web.UI.Page page, string imgPath)
    {
        HtmlMeta metaTagImg = new HtmlMeta();
        metaTagImg.Attributes.Add("property", "og:image");
        metaTagImg.Attributes.Add("content", imgPath);
        page.Header.Controls.Add(metaTagImg);
    }

    private static void AddMetaData(string propertyName, string content)
    {
        if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(content))
            return;
        if (HttpContext.Current.CurrentHandler is System.Web.UI.Page)
        {
            System.Web.UI.Page pg = (System.Web.UI.Page)HttpContext.Current.CurrentHandler;
            HtmlMeta metatag = new HtmlMeta();
            metatag.Attributes.Add("property", propertyName);
            metatag.Attributes.Add("content", content);
            pg.Header.Controls.Add(metatag);
        }
    }

    public string getImage(bool ShowExcerpt, string input)
    {
        if (!ShowExcerpt || input == null)
            return string.Empty;

        string pain = input;
        string pattern = @"<img(.|\n)+?>";
        System.Text.RegularExpressions.Match m =
            System.Text.RegularExpressions.Regex.Match(input, pattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);

        if (m.Success)
        {
            string src = getSrc(m.Value);
            return src;
        }
        else
        {
            return "";
        }
    }

    string getSrc(string input)
    {
        string pattern = "src=[\'|\"](.+?)[\'|\"]";
        System.Text.RegularExpressions.Regex reImg = new System.Text.RegularExpressions.Regex(pattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
        System.Text.RegularExpressions.Match mImg = reImg.Match(input);
        if (mImg.Success)
        {
            return mImg.Value.Replace("src=", "").Replace("\"", ""); ;
        }

        return string.Empty;
    }
}