using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using com.tiglobalsas.frontend.Utilities;
using SautinSoft.Document;
using SkiaSharp;
using System.Buffers.Text;
using System;


namespace com.tiglobalsas.frontend.Pages
{
    public class FileUploadFisicoModel : PageModel
    {
        private readonly long _fileSizeLimit;
        private readonly string[] _permittedExtensions = { ".doc", ".docx" };
        private readonly string _targetFilePath;

        public FileUploadFisicoModel(IConfiguration config)
        {
            _fileSizeLimit = config.GetValue<long>("FileSizeLimit");

            // para guardar los archivos se parametrizan en el json configuration.
            _targetFilePath = config.GetValue<string>("StoredFilesPath");

            bool folderExists = Directory.Exists(_targetFilePath);
            if (!folderExists)
            {
                Directory.CreateDirectory(_targetFilePath);
            }

        }

        [BindProperty]
        public FileUploadFisico FileUpload { get; set; }

        public string Result { get; private set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (!ModelState.IsValid)
            {
                Result = "Por favor corregir el archivo.";

                return Page();
            }

            var memoryStream = new MemoryStream();
            FileUpload.FormFile.CopyTo(memoryStream);

            var sourcePathFile = Path.Combine(_targetFilePath, FileUpload.FormFile.FileName);
            
            //Validar y confirmar archivo seleccionado
            var formFileContent = await FileHelpers.ProcessFormFile<FileUploadFisico>(FileUpload.FormFile, ModelState, _permittedExtensions, 
                    _fileSizeLimit, _targetFilePath);

            if (!ModelState.IsValid)
            {
                Result = "El modelo no es correcto.";

                return Page();
            }

            //Genera copia en un archivo random
            var trustedFileNameForFileStorage = Path.GetRandomFileName();
            var filePath = Path.Combine(
                _targetFilePath, trustedFileNameForFileStorage);
            var filePathTemp = Path.Combine(
                _targetFilePath, sourcePathFile);

            //Crear archivos y copia del archivo seleccionado
            using (var fileStream = System.IO.File.Create(filePath))
            {
                await fileStream.WriteAsync(formFileContent);
            }
            using (var fileStream = System.IO.File.Create(filePathTemp))
            {
                await fileStream.WriteAsync(formFileContent);
            }

            //Funcion que permite leer la pagina 1 la convierto en imagen.
            string filePathImg = @sourcePathFile;
            string filePathPDF = @sourcePathFile;
            DocumentCore dc = DocumentCore.Load(filePathImg);
            string folderPath = Path.GetFullPath(_targetFilePath);

            DocumentPaginator dp = dc.GetPaginator();

            for (int i = 0; i < dp.Pages.Count; i++)
            {
                DocumentPage page = dp.Pages[0];

                var DPI = new ImageSaveOptions();
                DPI.DpiX = 72;
                DPI.DpiY = 72;

                SKBitmap image = page.Rasterize(DPI, SautinSoft.Document.Color.White);

                Directory.CreateDirectory(folderPath);
                image.Encode(new FileStream(folderPath + @"\" + FileUpload.FormFile.FileName.Split(".")[0] + i.ToString() + DateTime.Now.ToString("HHmmss") + ".png", FileMode.Create), SkiaSharp.SKEncodedImageFormat.Png, 100);

                break;

            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folderPath) { UseShellExecute = true });
            //Fin de funcion que convierte a imagen.


            //Funcion para convertir a Base64 el archivo word.
            string base64 = String.Empty;
            byte[] binarydata = formFileContent;
            var trustedFileNameForFileStorageBase64 = Path.GetRandomFileName();
            base64 = System.Convert.ToBase64String(binarydata, 0, binarydata.Length);
            var filePath64 = Path.Combine(_targetFilePath, trustedFileNameForFileStorageBase64);

            using (var fileStream = System.IO.File.Create(filePath64))
            {
                await fileStream.WriteAsync(binarydata);
            }
            //Redireccionamos al index para seleccionar otro archivo

            //Funcion que permite leer la pagina 1 y la convierte a pdf
            using (MemoryStream msInp = new MemoryStream(binarydata))
            {
                byte[] outData = null;
                string outFile = folderPath + @"\" + FileUpload.FormFile.FileName.Split(".")[0] + DateTime.Now.ToString("HHmmss") + ".pdf";
                // Load a document.
                DocumentCore dcpdf = DocumentCore.Load(msInp, new DocxLoadOptions());
                //Calculo las paginas
                DocumentPaginator dpdf = dcpdf.GetPaginator();

                DocumentPage page = dpdf.Pages[0];

                
                //Guardo el documento de la primera pagina em formato PDF.
                using (MemoryStream outMs = new MemoryStream())
                {
                    page.Save(outMs, new PdfSaveOptions());
                    outData = outMs.ToArray();
                }

                if (outData != null)
                {
                    System.IO.File.WriteAllBytes(outFile, outData);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(outFile) { UseShellExecute = true });
                }
            }
            //Fin funcion que convierte a pdf

            return RedirectToPage("Index");
        }
    }

    public class FileUploadFisico
    {
        [Required]
        [Display(Name="Por favor seleccione archivo")]
        public IFormFile FormFile { get; set; }

        [Display(Name="Note")]
        [StringLength(50, MinimumLength = 0)]
        public string Note { get; set; }
    }
}
