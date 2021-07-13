using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using iTextSharp.tool.xml.html;
using System.Text;
using iTextSharp.tool.xml.pipeline.html;
using iTextSharp.tool.xml;
using iTextSharp.tool.xml.parser;
using iTextSharp.tool.xml.css;
using iTextSharp.tool.xml.pipeline.css;
using iTextSharp.tool.xml.pipeline.end;

namespace GeneratePDF.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private IHostingEnvironment Environment;
        //public WeatherForecastController()
        //{
        //    Environment = _environment;
        //}
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IHostingEnvironment _environment)
        {
            _logger = logger;
            Environment = _environment;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [Route("/htmlpdf")]
        [HttpGet]
        public FileStreamResult DownloadPDF()
        {
            string path = Path.Combine(this.Environment.ContentRootPath, "PDFTemplateContent/pdftemplate.html");
            string font = Path.Combine(this.Environment.ContentRootPath, "PDFTemplateContent");
            string HTMLContent = System.IO.File.ReadAllText(path);// Put your html tempelate here

            MemoryStream ms = new MemoryStream();
            TextReader txtReader = new StringReader(HTMLContent);

            // 1: create object of a itextsharp document class  
            Document doc = new Document(PageSize.A4, 25, 25, 25, 25);

            // 2: we create a itextsharp pdfwriter that listens to the document and directs a XML-stream to a file  
            PdfWriter pdfWriter = PdfWriter.GetInstance(doc, ms);
            pdfWriter.CloseStream = false;

            // 3: we create a worker parse the document  
            //HTMLWorker htmlWorker = new HTMLWorker(doc);

            // 4: we open document and start the worker on the document  
            doc.Open();
            //htmlWorker.StartDocument();
           

            // 5: parse the html into the document  
            //htmlWorker.Parse(txtReader);

            // 6: close the document and the worker  
            //htmlWorker.EndDocument();
            //htmlWorker.Close();

            // string font = HostingEnvironment.MapPath("~/Content/");
            var tagProcessors = (DefaultTagProcessorFactory)Tags.GetHtmlTagProcessorFactory();
            tagProcessors.RemoveProcessor(HTML.Tag.IMG); // remove the default processor
            //tagProcessors.AddProcessor(HTML.Tag.IMG, new CustomImageTagProcessor()); // use our new processor

            var cssFiles = new CssFilesImpl();
            cssFiles.Add(XMLWorkerHelper.GetInstance().GetDefaultCSS());

            var cssResolver = new StyleAttrCSSResolver(cssFiles);
            //cssResolver.AddCss(File.ReadAllText(templateCssFile), "utf-8", true);

            var fontProvider = new XMLWorkerFontProvider(XMLWorkerFontProvider.DONTLOOKFORFONTS);
            //fontProvider.Register(Path.Combine(font, "WINGDNG2.TTF"), "WINGDING");
            //fontProvider.Register(Path.Combine(font, "WINGDNG3.TTF"), "WINGDING3");

            var charset = Encoding.UTF8;
            var hpc = new HtmlPipelineContext(new CssAppliersImpl(fontProvider));
            hpc.SetAcceptUnknown(false).AutoBookmark(false).SetTagFactory(tagProcessors); // inject the tagProcessors

            var htmlPipeline = new HtmlPipeline(hpc, new PdfWriterPipeline(doc, pdfWriter));
            var pipeline = new CssResolverPipeline(cssResolver, htmlPipeline);
            var worker = new XMLWorker(pipeline, true);

            var xmlParser = new XMLParser(true, worker, charset);
            xmlParser.Parse(new StringReader(HTMLContent));

            doc.Close();

            ms.Flush(); //Always catches me out
            ms.Position = 0; //Not sure if this is required

            return File(ms, "application/pdf", "HelloWorld.pdf");
        }
    }
}
