namespace Digital_Mall_API.Services
{
    public class FileService
    {
        private readonly IWebHostEnvironment _env;

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file");

            // إنشاء فولدر التخزين (لو مش موجود)
            var uploadsPath = Path.Combine(_env.WebRootPath, folderName);
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            // اسم الملف الجديد (Guid + Extension)
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsPath, fileName);

            // نسخ الملف
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // إرجاع المسار كـ URL (يبدأ من wwwroot)
            return $"/{folderName}/{fileName}";
        }

        public bool DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;

            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }

            return false;
        }
    }
}
