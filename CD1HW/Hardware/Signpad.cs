using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Florentis;
using stdole;
using System.Threading;
using Windows.Networking;

namespace Code1HWSvr
{
    /// <summary>
    /// wacom signpad with signature sdk 
    /// sdk가 추가기능에 필요한 기능을 지원하지 않아 사용 x
    /// (수동으로 펜의 입력 포인트를 받을 필요가 있었으나 지원하지않음)
    /// </summary>
    class Signpad
    {
        private static bool ScriptIsRunning;               // flag for UI button respones
        private static WizardCallback Callback;            // For wizard callback 
        private static tPad Pad;                           // Pad parameters
        private static Thread fi;

        // 서명패드 설정값 저장 inner class (차후 추가모델 고려)
        class tPad
        {
            public string Model; // pad model name
            public IFontDisp fontSmall; // small font
            public IFontDisp fontmedium; // medium font
            public IFontDisp fontButton; // button font
            public int buttonWidth; // button width

            // 생성자
            public tPad(string model, int smallFontSize, int mediumFontSize, int buttonFontSize, int buttonWidth)
            {
                this.Model = model;
                this.buttonWidth = buttonWidth;
                this.fontSmall = SetFontProperties("맑은고딕", smallFontSize, false);
                this.fontmedium = SetFontProperties("맑은고딕", mediumFontSize, false);
                this.fontButton = SetFontProperties("맑은고딕", buttonFontSize, true);
            }

            // 폰트 지정
            private IFontDisp SetFontProperties(string fontName, int fontSize, bool isBold)
            {
                stdole.IFontDisp fnt = (stdole.IFontDisp)new stdole.StdFont();
                fnt.Name = fontName;
                fnt.Size = (decimal)fontSize;
                fnt.Bold = isBold;

                return fnt;
            }
        }

        private static WizCtl WizCtl;
        private static SigCtl SigCtl;
        private static string signPadImgPath;

        // init sign pad
        static Signpad()
        {
            SigCtl = new SigCtl();
            WizCtl = new WizCtl();
            
            // this licence 4 demo
            WizCtl.Licence = "eyJhbGciOiJSUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiI3YmM5Y2IxYWIxMGE0NmUxODI2N2E5MTJkYTA2ZTI3NiIsImV4cCI6MjE0NzQ4MzY0NywiaWF0IjoxNTYwOTUwMjcyLCJyaWdodHMiOlsiU0lHX1NES19DT1JFIiwiU0lHQ0FQVFhfQUNDRVNTIl0sImRldmljZXMiOlsiV0FDT01fQU5ZIl0sInR5cGUiOiJwcm9kIiwibGljX25hbWUiOiJTaWduYXR1cmUgU0RLIiwid2Fjb21faWQiOiI3YmM5Y2IxYWIxMGE0NmUxODI2N2E5MTJkYTA2ZTI3NiIsImxpY191aWQiOiJiODUyM2ViYi0xOGI3LTQ3OGEtYTlkZS04NDlmZTIyNmIwMDIiLCJhcHBzX3dpbmRvd3MiOltdLCJhcHBzX2lvcyI6W10sImFwcHNfYW5kcm9pZCI6W10sIm1hY2hpbmVfaWRzIjpbXX0.ONy3iYQ7lC6rQhou7rz4iJT_OJ20087gWz7GtCgYX3uNtKjmnEaNuP3QkjgxOK_vgOrTdwzD-nm-ysiTDs2GcPlOdUPErSp_bcX8kFBZVmGLyJtmeInAW6HuSp2-57ngoGFivTH_l1kkQ1KMvzDKHJbRglsPpd4nVHhx9WkvqczXyogldygvl0LRidyPOsS5H2GYmaPiyIp9In6meqeNQ1n9zkxSHo7B11mp_WXJXl0k1pek7py8XYCedCNW5qnLi4UCNlfTd6Mk9qz31arsiWsesPeR9PN121LBJtiPi023yQU8mgb9piw_a-ccciviJuNsEuRDN3sGnqONG3dMSA";

            Callback = new WizardCallback();    // Callback provided via InteropAXFlWizCOM
            Callback.EventHandler = null;
            WizCtl.SetEventHandler(Callback);
            ScriptIsRunning = false;
        }

        // start sign pad script
        private static void StartWizard(string name, string dob, string addr)
        {
            try
            {
                if (name == null || name.Equals(""))
                    name = " ";
                if (dob == null || dob.Equals(""))
                    dob = " ";
                if (addr == null)
                    addr = "";
                lock (Callback)
                {
                    bool isPadConnect = WizCtl.PadConnect();
                    if (!isPadConnect)
                    {
                        //logger.Fatal("failed connect signpad!!!");
                        //return;
                    }
                    switch (WizCtl.PadWidth)
                    {
                        case 396:
                            // STU-300 396 x 100
                            break;
                        case 640:
                            // STU-500 640 x 800
                            break;
                        case 800:
                            Pad = new tPad("STU-520 or STU-530", 30, 40, 45, 265); // 800 x 480
                            break;
                        case 320:
                            // STU-430 or ePad 320 x 200
                            break;
                        default:
                            break;
                    }

                    // create background image
                    Bitmap bitmap = new Bitmap(800, 480);
                    Graphics graphics = Graphics.FromImage(bitmap);
                    Rectangle imageSize = new Rectangle(0, 0, 800, 480);
                    graphics.FillRectangle(Brushes.White, imageSize);

                
                    /*SolidBrush solidBrush = new SolidBrush(Color.Black);
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.DrawString(name, font, solidBrush, 50, 120);

                    solidBrush = new SolidBrush(Color.White);
                    font = new System.Drawing.Font("궁서", 150, FontStyle.Regular);
                    graphics.DrawString(name, font, solidBrush, 50, 120);*/

                    // 속이 비어있는 글씨 그리기
                    GraphicsPath graphicsPath = new GraphicsPath();
                    Pen pen = new Pen(Brushes.Gray);
                    pen.Width = 3.0f;

                    Point pointOrigin;
                    //한글이름/영문이름 판별
                    string pattern = @"[^ㄱ-ㅎ|^ㅏ-ㅣ|^가-힣]";
                    Regex rgx = new Regex(pattern);

                    float fontSize = 180f;
                    System.Drawing.Font font = new System.Drawing.Font("돋움체", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                    // 영문 이름의 경우 (한글이 아닌 문자가 포함되어있을경우)
                    if (rgx.IsMatch(name))
                    {
                        string fstName = name.Split(' ')[0];
                        // padding width min 30px
                        float cvSize = 740f;
                        SizeF fstNameGpSize;
                        do
                        {
                            fstNameGpSize = graphics.MeasureString(fstName, font, new PointF(0,0), StringFormat.GenericTypographic);
                            fontSize = fontSize -= 5f;
                            font = new System.Drawing.Font("돋움체", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                        }
                        while (cvSize < fstNameGpSize.Width);

                        pointOrigin = new Point((800-(int)fstNameGpSize.Width)/2, (480-(int)fstNameGpSize.Height)/2);
                    }
                    // 한글 이름인 경우
                    else
                    {
                        // 이름 글자수에 따라 그리는 위치 변경
                        /*if (name.Length > 4)
                        {
                            SizeF nameGpSize = graphics.MeasureString(name, font, new PointF(0, 0), StringFormat.GenericTypographic);
                            fontSize = fontSize -= 5f;
                            font = new System.Drawing.Font("돋움체", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                            pointOrigin = new Point((800 - (int)nameGpSize.Width) / 2, (480 - (int)nameGpSize.Height) / 2);
                        }
                        else if (name.Length == 4)
                        {
                            pointOrigin = new Point(200, 150);
                        }
                        else if(name.Length == 3)
                        {
                            pointOrigin = new Point(120, 150);
                        }
                        else
                        {
                            pointOrigin = new Point(30, 150);
                        }*/

                        SizeF nameGpSize = graphics.MeasureString(name, font, new PointF(0, 0), StringFormat.GenericTypographic);
                        fontSize = fontSize -= 5f;
                        font = new System.Drawing.Font("돋움체", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                        pointOrigin = new Point((800 - (int)nameGpSize.Width) / 2, (480 - (int)nameGpSize.Height) / 2);

                        SizeF tsize = graphics.MeasureString(name, font, new PointF(0, 0), StringFormat.GenericTypographic);
                    }
                    graphicsPath.AddString(name, font.FontFamily, (int)FontStyle.Bold, fontSize, pointOrigin, StringFormat.GenericTypographic);

                    graphics.DrawPath(pen, graphicsPath);
                    //graphics.DrawString(name, font, Brushes.Black, pointOrigin, StringFormat.GenericTypographic );

                    // 주소그리기
                    string[] addrArr = new string[2];
                    if (addr.Length < 17)
                    {
                        addrArr[0] = addr;
                    }
                    else
                    {
                        addrArr[0] = addr.Substring(0, ((int)addr.Length/2));
                        if(addr.Length %2 == 0)
                        {
                            addrArr[1] = addr.Substring(((int)addr.Length / 2), ((int)addr.Length / 2));
                        }
                        else
                        {
                            addrArr[1] = addr.Substring(((int)addr.Length / 2), ((int)addr.Length / 2) + 1);
                        }
                    }
                    float addrFontSize = 50f;
                    System.Drawing.Font addrFont = new System.Drawing.Font("굴림체", addrFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                    float cvSize2 = 780;
                    SizeF fstAddrGpSize;
                    do
                    {
                        fstAddrGpSize = graphics.MeasureString(addrArr[1], addrFont, new PointF(0, 0), StringFormat.GenericTypographic);
                        addrFontSize = addrFontSize -= 2f;
                        addrFont = new System.Drawing.Font("굴림체", addrFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                    }
                    while (cvSize2 < fstAddrGpSize.Width);

                    pointOrigin = new Point(10, 50);
                    graphics.DrawString(addrArr[0], addrFont, Brushes.Gray, pointOrigin);
                    pointOrigin = new Point(10, 100);
                    graphics.DrawString(addrArr[1], addrFont, Brushes.Gray, pointOrigin);





                    byte[] byteArr = null;
                    if (bitmap != null)
                    {
                        MemoryStream stream = new MemoryStream();
                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                        byteArr = stream.ToArray();
                    }
                
                   /* object position
                    X: "left", "right", "centre"
                    Y: "top", "middle", "bottom"
                    or absolute position in pixels*/
                    WizCtl.Reset();

                    // 상단부 text. who, why id를 가지는 ObjectText는 꼭 존재해야함 (Signature SDK의 사양)
                    WizCtl.Font = Pad.fontSmall;
                    WizCtl.AddObject(ObjectType.ObjectImage, "", 0, 0, byteArr, null);
                    WizCtl.AddObject(ObjectType.ObjectText, "who", "left", "top", name, null);
                    WizCtl.AddObject(ObjectType.ObjectText, "why", "right", "top", dob, null);
                    WizCtl.Font = Pad.fontmedium;
                    // 주소가 너무 길면 자른다.
                    //if (addr.Length > 18)
                    //    addr = addr.Substring(0, 16) + "...";
                    //WizCtl.AddObject(ObjectType.ObjectText, "txt", "left", 50, addr, null);

                    WizCtl.AddObject(ObjectType.ObjectSignature, "Sig", 0, 0, SigCtl.Signature, null);

                    WizCtl.Font = Pad.fontButton;

                    // 버튼 옵션
                    ObjectOptions buttonOptions = new ObjectOptions();
                    buttonOptions.SetProperty("Height", 100);
                    buttonOptions.SetProperty("Width", Pad.buttonWidth);

                    // 버튼 배경색 r,g,b (1.0 = 255)
                    WizCtl.SetProperty("ObjectBackgroundColor", "1.0,0.8,0.6");
                    WizCtl.AddObject(ObjectType.ObjectButton, "OK", "left", "bottom", "확인", buttonOptions);
                    WizCtl.SetProperty("ObjectBackgroundColor", "1.0,1.0,0.8");
                    WizCtl.AddObject(ObjectType.ObjectButton, "Clear", "centre", "bottom", "지우기", buttonOptions);
                    WizCtl.SetProperty("ObjectBackgroundColor", "1.0,0.6,0.8");
                    WizCtl.AddObject(ObjectType.ObjectButton, "Cancel", "right", "bottom", "취소", buttonOptions);

                    // 버튼 클릭시 event handler 설정
                    Callback.EventHandler = new WizardCallback.Handler(signDispHandler);
                    WizCtl.SetEventHandler(Callback);
                    WizCtl.Display();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                //logger.Fatal(e.StackTrace);
            }

        }

        // sign pad 버튼 클릭시 event handler
        private static void signDispHandler(object clt, object id, object type)
        {
            switch (id.ToString())
            {
                case "OK":
                    {
                        scriptCompleted();
                        //fi.Abort();
                        fi.Interrupt();
                        fi.Join();
                        break;
                    }
                case "Clear":
                    {
                        break;
                    }
                case "Cancel":
                    {
                        scriptCancelled();
                        //fi.Abort();
                        fi.Interrupt();
                        fi.Join();
                        
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        // 확인버튼 클릭시 이미지 저장 > 서명패드 종료
        private static void scriptCompleted()
        {
            try
            {
                SigObj sigObj = (SigObj)SigCtl.Signature;
                if (sigObj.IsCaptured)
                {
                    sigObj.set_ExtraData("AdditionalData", "Sign Image");
                    // 서명 이미지 저장
                    sigObj.RenderBitmap(signPadImgPath, 400, 300, "image/png", 2.0f, 0x000000, 0xffffff, -1.0f, -1.0f, RBFlags.RenderOutputFilename | RBFlags.RenderColor32BPP | RBFlags.RenderEncodeData);
                }
            }
            catch (Exception e)
            {
                //logger.Fatal(e.StackTrace);
            }
            closeWizard();
        }
        private static void scriptCancelled()
        {
            closeWizard();
        }

        // 서명패드 종료
        public static void closeWizard()
        {
            lock (Callback)
            {
                WizCtl.Reset();
                WizCtl.Display();
                WizCtl.PadDisconnect();
                Callback.EventHandler = null;       // remove handler
                WizCtl.SetEventHandler(Callback);
            }
        }

        // test function
        public static void PadTest()
        {
            //CallSignPadEvent("홍길동", "1586.07.29", "서울시 동해인지 서해인지", "./sign_test.png");
        }

        // 외부에서 호출위한 function
        // 임시생성되는 sign image 이미지 지정
        public static void CallSignPadEvent(string name, string addr, string dob, string signPadImgPath, Thread fiThread)
        {
            Signpad.signPadImgPath = signPadImgPath;
            StartWizard(name, addr, dob);
            fi = fiThread;
        }
    }
}
