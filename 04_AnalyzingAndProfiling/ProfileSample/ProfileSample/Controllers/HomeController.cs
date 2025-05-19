using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProfileSample.DAL;
using ProfileSample.Models;

namespace ProfileSample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var context = new ProfileSampleEntities())
            {
                var model = context.ImgSources
                    .Take(20)
                    .Select(x => new ImageModel
                    {
                        Name = x.Name,
                        Data = x.Data
                    })
                    .ToList();

                return View(model);
            }
        }

        public ActionResult Convert()
        {
            var files = Directory.GetFiles(Server.MapPath("~/Content/Img"), "*.jpg");
            const int bufferSize = 81920;

            using (var context = new ProfileSampleEntities())
            {
                foreach (var file in files)
                {
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        var entity = new ImgSource
                        {
                            Name = Path.GetFileName(file),
                            Data = new byte[stream.Length]
                        };

                        int bytesRead;
                        int totalBytesRead = 0;
                        var buffer = new byte[bufferSize];

                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            Buffer.BlockCopy(buffer, 0, entity.Data, totalBytesRead, bytesRead);
                            totalBytesRead += bytesRead;
                        }

                        context.ImgSources.Add(entity);
                    }
                }
                context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}