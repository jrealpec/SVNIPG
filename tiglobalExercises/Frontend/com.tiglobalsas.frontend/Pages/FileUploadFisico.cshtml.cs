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

            // To save physical files to a path provided by configuration:
            _targetFilePath = config.GetValue<string>("StoredFilesPath");

            // To save physical files to the temporary files folder, use:
            //_targetFilePath = Path.GetTempPath();
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
                image.Encode(new FileStream(folderPath + @"\" + FileUpload.FormFile.FileName.Split(".")[0] + i.ToString() + ".png", FileMode.Create), SkiaSharp.SKEncodedImageFormat.Png, 100);


            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folderPath) { UseShellExecute = true });

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
