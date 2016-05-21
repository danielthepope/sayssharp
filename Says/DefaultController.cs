using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;

namespace Says
{
    public class DefaultController : ApiController
    {
        public HttpResponseMessage Get(string who, string message)
        {
            who = who.ToLower();
            string root = HttpRuntime.AppDomainAppPath;
            dynamic people = JsonConvert.DeserializeObject(File.ReadAllText(root + "people.json"));
            string image = people[who].image;
            int fontSize = people[who].fontSize;
            dynamic points = people[who].screen;
            dynamic textColour = people[who].textColour;
            bool widescreen = people[who].widescreen;
            Bitmap textImage = DrawText(message, new Font(FontFamily.GenericSansSerif, fontSize),
                Color.FromArgb((int)textColour[0], (int)textColour[1], (int)textColour[2]), Color.Transparent, widescreen);
            YLScsDrawing.Imaging.Filters.FreeTransform filter = new YLScsDrawing.Imaging.Filters.FreeTransform();
            filter.Bitmap = textImage;
            // assign FourCorners (the four X/Y coords) of the new perspective shape
            filter.FourCorners = new PointF[] {
                new System.Drawing.PointF((float)points[0], (float)points[1]),
                new System.Drawing.PointF((float)points[2], (float)points[3]),
                new System.Drawing.PointF((float)points[4], (float)points[5]),
                new System.Drawing.PointF((float)points[6], (float)points[7])
            };
            filter.IsBilinearInterpolation = true; // optional for higher quality
            using (Bitmap background = new Bitmap(root + "images\\" + image))
            {
                using (Bitmap perspectiveImg = filter.Bitmap)
                {
                    Bitmap finalImage = Superimpose(background, perspectiveImg, points);
                    return ReturnImage(finalImage);
                    // perspectiveImg contains your completed image. save the image or do whatever.
                }
            }

        }

        private Bitmap DrawText(String text, Font font, Color textColor, Color backColor, bool widescreen)
        {
            //first, create a dummy bitmap just to get a graphics object
            Bitmap img = new Bitmap(1, 1);
            Graphics drawing = Graphics.FromImage(img);
            int targetWidth = widescreen ? 1366 : 1024;
            int targetHeight = 768;

            //measure the string to see how big the image needs to be
            SizeF textSize = drawing.MeasureString(text, font, new SizeF(targetWidth, targetHeight));

            //free up the dummy image and old graphics object
            img.Dispose();
            drawing.Dispose();

            //create a new image of the right size
            img = new Bitmap(targetWidth, targetHeight);

            drawing = Graphics.FromImage(img);

            //paint the background
            drawing.Clear(backColor);

            //create a brush for the text
            Brush textBrush = new SolidBrush(textColor);

            drawing.DrawString(text, font, textBrush,
                new RectangleF(
                    (targetWidth - textSize.Width) / 2.0F,
                    (targetHeight - textSize.Height) / 2.0F,
                    textSize.Width,
                    textSize.Height
                )
            );

            drawing.Save();

            textBrush.Dispose();
            drawing.Dispose();

            return img;

        }

        private HttpResponseMessage ReturnImage(Image image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new ByteArrayContent(ms.ToArray());
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return result;
        }

        private Bitmap Superimpose(Bitmap largeBmp, Bitmap smallBmp, dynamic screen)
        {
            int lw = largeBmp.Width;
            int lh = largeBmp.Height;
            int sw = smallBmp.Width;
            int sh = smallBmp.Height;
            Graphics g = Graphics.FromImage(largeBmp);
            g.CompositingMode = CompositingMode.SourceOver;
            g.DrawImage(smallBmp, new Rectangle(TopLeft(screen), new Size(sw, sh)));
            return largeBmp;
        }

        private Point TopLeft(dynamic points)
        {
            int minX = Math.Min((int)points[0], (int)points[6]);
            int minY = Math.Min((int)points[1], (int)points[3]);
            return new Point(minX, minY);
        }
    }
}