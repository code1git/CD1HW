namespace CD1HW
{
    // Appsettiongs.json을 통해 읽는 초기 config
    public class Appsettings
    {
        public const string Settings = "Appsettings";
        public bool DemoUIOnStart { get; set; }
        public int CamIdx { get; set; } = 0;
        public string CameraBackend { get; set; }
        public string NecDemoFilePath { get; set;}
        public string NecDemoFileType { get; set; }
        public string ProductType { get; set; }
        public string SignPadFont { get; set; }
        public string ResultPath { get; set; }
        public string IRMode { get; set; }
        public int IRTimesleep { get; set; }
        public bool CameraCrop { get; set; }
        public string CameraCropBBox { get; set; }
        public int CameraRotate { get; set; }

    }
}

