namespace CD1HW
{
    // Appsettiongs.json을 통해 읽는 초기 config
    public class AppSettings
    {
        public const string Settings = "AppSettings";
        public bool DemoUIOnStart { get; set; }
        public int CamIdx { get; set; }
        public string CameraBackend { get; set; }
        public string NecDemoFilePath { get; set;}
        public string NecDemoFileType { get; set; }
        public string ProductType { get; set; }
        public string SignPadFont { get; set; }
        public string ResultPath { get; set; }
    }
}

