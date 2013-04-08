using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace VIPS
{
    public partial class Form1 : Form
    {
        PaintEventHandler painter = null;

        public Form1()
        {
            InitializeComponent();

            string metapaintball = "http://paintball.fsmi.uni-karlsruhe.de/bilder", // separierbar ab 8
                metafacebook = "http://www.facebook.com/pages/Trust-me-Im-a-Biologist/138846579561292?sk=photos_albums", // nicht separierbar
                facebook = "http://www.facebook.com/pages/Trust-me-Im-a-Biologist/138846579561292?sk=photos", // nicht separierbar
                flickrexplore = "http://flickr.com/explore",
                picasa = "https://picasaweb.google.com/Paintball.Fsmi/2006Kai?authkey=Gv1sRgCISw9Kao6s6GRQ"; // separierbar ab 10

            webBrowser1.Navigate(picasa);
        }

        private void Analyze()
        {
            PAGEANALYZERLib.LayoutAnalyzer2 myAnalyzer = new PAGEANALYZERLib.LayoutAnalyzer2();
            myAnalyzer.Initialize(0);
            SHDocVw.IWebBrowser2 axbrowser = (SHDocVw.IWebBrowser2)webBrowser1.ActiveXInstance;
            int granularity = Int32.Parse(textBox2.Text);
            myAnalyzer.Analyze4(axbrowser.Document as Object, granularity);
            MSXML2.IXMLDOMDocument legacyResult = myAnalyzer.FOMPage as MSXML2.IXMLDOMDocument;
            var xresult = XDocument.Parse(legacyResult.xml);
            xresult.Save("segments.xml");
            //RebuildPage(XDocument.Parse(result.xml));
            RepaintPage(xresult);
        }

        private void RepaintPage(XDocument xDocument)
        {
            using(var tex = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics gfxScreenshot = Graphics.FromImage(tex)) // Create a graphics object from the bitmap
                {
                    Point browserloc = webBrowser1.PointToScreen(Point.Empty);

                    // Take the screenshot starting at the browsers location
                    gfxScreenshot.CopyFromScreen(browserloc.X, browserloc.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);

                    webBrowser1.Hide();

                    if (painter != null)
                    {
                        webBrowser1.Parent.Paint -= painter;
                    }
                    {
                        using (Pen myPen = new Pen(Color.Red))
                        {
                            using (var gfx = webBrowser1.Parent.CreateGraphics())
                            {
                                foreach (var node in xDocument.Descendants("LayoutNode"))
                                {
                                    Point loc = new Point(Int32.Parse(node.Attribute("ObjectRectLeft").Value), Int32.Parse(node.Attribute("ObjectRectTop").Value));
                                    Size siz = new Size(Int32.Parse(node.Attribute("ObjectRectWidth").Value), Int32.Parse(node.Attribute("ObjectRectHeight").Value));
                                    Rectangle rec = new Rectangle(loc, siz);

                                    gfx.FillRectangle(new TextureBrush(tex), rec);
                                    gfx.DrawRectangle(myPen, rec);
                                }
                            }
                        }
                    }
                }
                //webBrowser1.Parent.Paint += painter;
            }
            //webBrowser1.Hide();
        }

        private void RebuildPage(XDocument result)
        {
            // do something
            webBrowser1.Hide();

            var allPanels = new FlowLayoutPanel();
            allPanels.Dock = DockStyle.Fill;
            allPanels.AutoScroll = true;
            
            foreach (var node in result.Descendants("LayoutNode"))
            {
                Debug.WriteLine(node);
                if (node.Descendants("LayoutNode").Count() == 0)
                {
                    var segment = new Panel();
                    
                    segment.Width = Int32.Parse(node.Attribute("ObjectRectWidth").Value);
                    segment.Height = Int32.Parse(node.Attribute("ObjectRectHeight").Value);
                    segment.Controls.Add(new WebBrowser() { 
                        DocumentText = System.Net.WebUtility.HtmlDecode(node.Attribute("SRC").Value),
                        Dock = DockStyle.Fill
                    });
                    allPanels.Controls.Add(segment);
                }
            }
            Debug.Write("allPanels has " + allPanels.Controls.Count + " children.");
            
            this.Controls.Add(allPanels);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            Analyze();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                webBrowser1.Show();
                webBrowser1.Navigate(textBox1.Text);
                
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            button1.Enabled = true;
            textBox1.Text = webBrowser1.Url.AbsoluteUri;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            webBrowser1.Show();
            button1.Enabled = true;
        }
    }
}
