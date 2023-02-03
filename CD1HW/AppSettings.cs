namespace CD1HW
{
    public class AppSettings
    {
        public const string Settings = "AppSettings";
        public bool DemoUIOnStart { get; set; }
        public int CamIdx { get; set; }
        public string CameraBackend { get; set; }
        public string NecDemoFilePath { get; set;}
    }
}

