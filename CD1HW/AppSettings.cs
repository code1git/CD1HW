using CD1HW.Hardware;

namespace CD1HW
{
    public class AppSettings
    {
        private static readonly Lazy<AppSettings> _insteance = new Lazy<AppSettings>(() => new AppSettings());
        public static AppSettings Instance { get { return _insteance.Value; } }
        public int camIdx { get; set; } = 0;
        public string imgBase64Str { get; set; }
        public Bitmap cameraBitmap { get; set; }

        //ocr 결과
        public string id_card_type { get; set; }
        public string name { get; set; }
        public string regnum { get; set; }
        public string driver_num { get; set; }
        public string birth { get; set; }
        public string issue_date { get; set; }
        public string addr { get; set; }

        //ocr 결과 image crop
        public string name_img { get; set; }
        public string regnum_img { get; set; }
        public string face_img { get; set; }
        public string birth_img { get; set; }

    }
}
