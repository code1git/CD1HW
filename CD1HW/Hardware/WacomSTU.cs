using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Drawing.Drawing2D;
using Windows.Devices.Usb;

namespace CD1HW.Hardware
{
    public class WacomSTU
    {
        private readonly ILogger<WacomSTU> _logger;
        private readonly OcrCamera _ocrCamera;
        //private readonly AudioDevice _audioDevice;

        public WacomSTU(ILogger<WacomSTU> logger, OcrCamera ocrCamera)
        {
            _logger = logger;
            _ocrCamera = ocrCamera;
            //_audioDevice = AudioDevice.Instance;
        }

        /*
         * 0 : wait
         * 1 : completed
         * 2 : cansoled
         */
        public int completeFlag = -1;

        enum PenDataOptionMode
        {
            PenDataOptionMode_None,
            PenDataOptionMode_TimeCount,
            PenDataOptionMode_SequenceNumber,
            PenDataOptionMode_TimeCountSequence
        };


        public int penDataType;
        private wgssSTU.Tablet m_tablet;
        private wgssSTU.ICapability m_capability;
        private wgssSTU.IInformation m_information;
        private wgssSTU.ProtocolHelper protocolHelper;
        private Bitmap signImage;

        // In order to simulate buttons, we have our own Button class that stores the bounds and event handler.
        // Using an array of these makes it easy to add or remove buttons as desired.
        private delegate void ButtonClick();

        private struct Button
        {
            public Rectangle Bounds; // in Screen coordinates
            public String Text;
            public EventHandler Click;

            public void PerformClick()
            {
                Click(this, null);
            }
        };

        private Pen m_penInk;  // cached object.

        // The isDown flag is used like this:
        // 0 = up
        // +ve = down, pressed on button number
        // -1 = down, inking
        // -2 = down, ignoring
        private int m_isDown;

        private List<wgssSTU.IPenData> m_penData; // Array of data being stored. This can be subsequently used as desired.
        private List<wgssSTU.IPenDataTimeCountSequence> m_penTimeData; // Array of data being stored. This can be subsequently used as desired.

        private Button[] m_btns; // The array of buttons that we are emulating.

        private Bitmap m_bitmap; // This bitmap that we display on the screen.
        private wgssSTU.encodingMode m_encodingMode; // How we send the bitmap to the device.
        private byte[] m_bitmapData; // This is the flattened data of the bitmap that we send to the device.
        private int m_penDataOptionMode;  // The pen data option mode flag - basic or with time and sequence counts

        private bool m_useEncryption;
        private bool useColor;

        // As per the file comment, there are three coordinate systems to deal with.
        // To help understand, we have left the calculations in place rather than optimise them.

        private Point tabletToScreen(wgssSTU.IPenData penData)
        {
            // Screen means LCD screen of the tablet.
            return Point.Round(new PointF((float)penData.x * m_capability.screenWidth / m_capability.tabletMaxX, (float)penData.y * m_capability.screenHeight / m_capability.tabletMaxY));
        }

        private void clearScreen()
        {
            // note: There is no need to clear the tablet screen prior to writing an image.

            if (m_useEncryption)
                m_tablet.endCapture();

            m_tablet.writeImage((byte)m_encodingMode, m_bitmapData);

            if (m_penDataOptionMode == (int)PenDataOptionMode.PenDataOptionMode_TimeCountSequence)
            {
                m_penTimeData.Clear();
            }
            else
            {
                m_penData.Clear();
            }

            if (m_useEncryption)
                m_tablet.startCapture(0xc0ffee);

            m_isDown = 0;



            
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            // You probably want to add additional processing here.
            penDataType = m_penDataOptionMode;
            int currentPenDataOptionMode = getPenDataOptionMode();
            setPenDataOptionMode(currentPenDataOptionMode);

            

            completeFlag = 1;
        }


        private void btnCancel_Click(object sender, EventArgs e)
        {
            int currentPenDataOptionMode = getPenDataOptionMode();
            setPenDataOptionMode(currentPenDataOptionMode);

            _ocrCamera.sign_img = null;

            completeFlag = 2;

        }


        private async void btnClear_Click(object sender, EventArgs e)
        {
            if (m_penData.Count != 0 || m_penTimeData.Count != 0)
            {
                clearScreen();
            }
            ResetPadImage();

        }

        public void StartPad()
        {
            _logger.LogInformation("start sign pad");
            List<wgssSTU.IPenDataTimeCountSequence> penTimeData = null;
            List<wgssSTU.IPenData> penData = null;
            wgssSTU.UsbDevices usbDevices = new wgssSTU.UsbDevices();
            try
            {
                if (usbDevices.Count != 0)
                {
                    wgssSTU.IUsbDevice usbDevice = usbDevices[0];
                    int currentPenDataOptionMode;
                    m_penDataOptionMode = -1;

                    m_penData = new List<wgssSTU.IPenData>();
                    m_tablet = new wgssSTU.Tablet();

                    protocolHelper = new wgssSTU.ProtocolHelper();

                    // A more sophisticated applications should cycle for a few times as the connection may only be
                    // temporarily unavailable for a second or so. 
                    // For example, if a background process such as Wacom STU Display
                    // is running, this periodically updates a slideshow of images to the device.

                    wgssSTU.IErrorCode ec = m_tablet.usbConnect(usbDevice, true);
                    if (ec.value == 0)
                    {
                        m_capability = m_tablet.getCapability();
                        m_information = m_tablet.getInformation();

                        // First find out if the pad supports the pen data option mode (the 300 doesn't)
                        currentPenDataOptionMode = getPenDataOptionMode();

                        // Set up the tablet to return time stamp with the pen data or just basic data
                        setPenDataOptionMode(currentPenDataOptionMode);
                    }
                    else
                    {
                        throw new Exception(ec.message);
                    }

                    m_btns = new Button[3];
                    if (usbDevice.idProduct != 0x00a2)
                    {
                        // Place the buttons across the bottom of the screen.

                        int w2 = m_capability.screenWidth / 3;
                        int w3 = m_capability.screenWidth / 3;
                        int w1 = m_capability.screenWidth - w2 - w3;
                        int y = m_capability.screenHeight * 6 / 7;
                        int h = m_capability.screenHeight - y;

                        m_btns[0].Bounds = new Rectangle(0, y, w1, h);
                        m_btns[1].Bounds = new Rectangle(w1, y, w2, h);
                        m_btns[2].Bounds = new Rectangle(w1 + w2, y, w3, h);
                    }
                    else
                    {
                        // The STU-300 is very shallow, so it is better to utilise
                        // the buttons to the side of the display instead.

                        int x = m_capability.screenWidth * 3 / 4;
                        int w = m_capability.screenWidth - x;

                        int h2 = m_capability.screenHeight / 3;
                        int h3 = m_capability.screenHeight / 3;
                        int h1 = m_capability.screenHeight - h2 - h3;

                        m_btns[0].Bounds = new Rectangle(x, 0, w, h1);
                        m_btns[1].Bounds = new Rectangle(x, h1, w, h2);
                        m_btns[2].Bounds = new Rectangle(x, h1 + h2, w, h3);
                    }
                    m_btns[0].Text = "확인";
                    m_btns[1].Text = "지우기";
                    m_btns[2].Text = "취소";
                    m_btns[0].Click = new EventHandler(btnOk_Click);
                    m_btns[1].Click = new EventHandler(btnClear_Click);
                    m_btns[2].Click = new EventHandler(btnCancel_Click);

                    // Disable color if the STU-520 bulk driver isn't installed.
                    // This isn't necessary, but uploading colour images with out the driver
                    // is very slow.

                    // Calculate the encodingMode that will be used to update the image
                    ushort idP = m_tablet.getProductId();
                    wgssSTU.encodingFlag encodingFlag = (wgssSTU.encodingFlag)protocolHelper.simulateEncodingFlag(idP, 0);
                    useColor = false;
                    if ((encodingFlag & (wgssSTU.encodingFlag.EncodingFlag_16bit | wgssSTU.encodingFlag.EncodingFlag_24bit)) != 0)
                    {
                        if (m_tablet.supportsWrite())
                            useColor = true;
                    }
                    if ((encodingFlag & wgssSTU.encodingFlag.EncodingFlag_24bit) != 0)
                    {
                        m_encodingMode = m_tablet.supportsWrite() ? wgssSTU.encodingMode.EncodingMode_24bit_Bulk : wgssSTU.encodingMode.EncodingMode_24bit;
                    }
                    else if ((encodingFlag & wgssSTU.encodingFlag.EncodingFlag_16bit) != 0)
                    {
                        m_encodingMode = m_tablet.supportsWrite() ? wgssSTU.encodingMode.EncodingMode_16bit_Bulk : wgssSTU.encodingMode.EncodingMode_16bit;
                    }
                    else
                    {
                        // assumes 1bit is available
                        m_encodingMode = wgssSTU.encodingMode.EncodingMode_1bit;
                    }


                    bool useZlibCompression = false;
                    if (!useColor && useZlibCompression)
                    {
                        // m_bitmapData = compress_using_zlib(m_bitmapData); // insert compression here!
                        m_encodingMode |= wgssSTU.encodingMode.EncodingMode_Zlib;
                    }

                    //SizeF s = this.AutoScaleDimensions;
                    float inkWidthMM = 0.7F;
                    //m_penInk = new Pen(Color.DarkBlue, inkWidthMM / 25.4F * ((s.Width + s.Height) / 2F));
                    m_penInk = new Pen(Color.DarkBlue, inkWidthMM / 25.4F * (400 / 2F));

                    m_penInk.StartCap = m_penInk.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    m_penInk.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;

                    Bitmap b1 = Properties.Resources.sign_start;
                    SetPadImage(b1);

                    addDelegates();

                    // Initialize the screen

                    //m_tablet.setInkingMode(0x01);
                    
                    

                    while (true)
                    {
                        Thread.Sleep(1000);
                    }

                }
                else
                {
                    _logger.LogError("No STU devices attached");
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.Message, e);
            }
        }

        public void SetPadImage(Bitmap image)
        {
            // Size the bitmap to the size of the LCD screen.
            // This application uses the same bitmap for both the screen and client (window).
            // However, at high DPI, this bitmap will be stretch and it would be better to 
            // create individual bitmaps for screen and client at native resolutions.
            m_bitmap = new Bitmap(m_capability.screenWidth, m_capability.screenHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            {
                Graphics gfx = Graphics.FromImage(m_bitmap);
                gfx.Clear(Color.White);
                gfx.DrawImage(image, 0, 0, 800, 480);
                gfx.Dispose();
            }
            // Now the bitmap has been created, it needs to be converted to device-native
            // format.
            {

                // Unfortunately it is not possible for the native COM component to
                // understand .NET bitmaps. We have therefore convert the .NET bitmap
                // into a memory blob that will be understood by COM.
                protocolHelper = new wgssSTU.ProtocolHelper();
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                m_bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                m_bitmapData = (byte[])protocolHelper.resizeAndFlatten(stream.ToArray(), 0, 0, (uint)m_bitmap.Width, (uint)m_bitmap.Height, m_capability.screenWidth, m_capability.screenHeight, (byte)m_encodingMode, wgssSTU.Scale.Scale_Fit, 0, 0);
                protocolHelper = null;
                stream.Dispose();
            }
            clearScreen();
            m_tablet.setInkingMode(0x00);
        }

        public void SetSignPad(string name, string birth, string addr)
        {
            completeFlag = 0;
            ResetPadImage();

            Bitmap bitmap = new Bitmap(800, 480);
            {
                if (name == null || name.Equals(""))
                    name = " ";
                if (birth == null || birth.Equals(""))
                    birth = " ";
                if (addr == null)
                    addr = "";
                Graphics graphics = Graphics.FromImage(bitmap);
                Rectangle imageSize = new Rectangle(0, 0, 800, 480);
                graphics.Clear(Color.White);

                GraphicsPath graphicsPath = new GraphicsPath();
                Pen pen = new Pen(Brushes.Gray);
                pen.Width = 3.0f;

                Point pointOrigin;

                float fontSize;
                //Font padFont = new Font("돋움체", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                Font padFont;
                string fontName = _ocrCamera.SignPadFont;
                float cvSize = 780f;

                

                // 주소그리기
                string[] addrArr = new string[5];
                if (addr.Length < 31)
                {
                    addrArr[0] = addr;
                }
                else if (name.Length < 61)
                {
                    /*if (addr.Length % 2 == 0)
                    {
                        addrArr[0] = addr.Substring(0, (int)(addr.Length / 2));
                        addrArr[1] = addr.Substring((int)(addr.Length / 2), (int)(addr.Length / 2));
                    }
                    else
                    {
                        addrArr[0] = addr.Substring(0, (int)(addr.Length / 2)+1);
                        addrArr[1] = addr.Substring((int)(addr.Length / 2)+1, (int)(addr.Length / 2));
                    }*/
                    addrArr[0] = addr.Substring(0, 30);
                    addrArr[1] = addr.Substring(30, addr.Length - 30);
                }
                else
                {
                    addrArr[0] = addr.Substring(0, 30);
                    addrArr[1] = addr.Substring(30, 30);
                    addrArr[2] = addr.Substring(60, addr.Length - 60);
                }
                fontSize = 30f;
                padFont = new Font(fontName, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                SizeF fontGpSize;
                do
                {
                    fontGpSize = graphics.MeasureString(addrArr[0], padFont, new PointF(0, 0), StringFormat.GenericTypographic);
                    fontSize = fontSize -= 1f;
                    padFont = new Font(fontName, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                }
                while (cvSize < fontGpSize.Width);

                if (addrArr[1] == null)
                {
                    pointOrigin = new Point(5, 60);
                    graphics.DrawString(addrArr[0], padFont, Brushes.Black, pointOrigin);
                }
                else
                {
                    pointOrigin = new Point(5, 42);
                    graphics.DrawString(addrArr[0], padFont, Brushes.Black, pointOrigin);
                    pointOrigin = new Point(5, 84);
                    graphics.DrawString(addrArr[1], padFont, Brushes.Black, pointOrigin);
                }

                graphics.DrawLine(Pens.Red, new Point(5, 130), new Point(795, 130));

                string[] nameArr = new string[5];
                if (name.Length < 23)
                {
                    nameArr[0] = name;
                }
                else
                {
                    if (name.Length % 2 == 0)
                    {
                        nameArr[0] = name.Substring(0, (int)(name.Length / 2));
                        nameArr[1] = name.Substring((int)(name.Length / 2), (int)(name.Length / 2));
                    }
                    else
                    {
                        nameArr[0] = name.Substring(0, (int)(name.Length / 2)+1);
                        nameArr[1] = name.Substring((int)(name.Length / 2)+1, (int)(name.Length / 2));
                    }

                }

                fontSize = 180f;
                padFont = new Font(fontName, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);

                // 이름 글자수에 따라 그리는 위치 변경

                SizeF nameGpSize;
                do
                {
                    nameGpSize = graphics.MeasureString(nameArr[0], padFont, new PointF(5, 130), StringFormat.GenericTypographic);
                    if (nameArr[1] != null)
                    {
                        SizeF tempSize = graphics.MeasureString(nameArr[1], padFont, new PointF(5, 130), StringFormat.GenericTypographic);
                        if(tempSize.Width > nameGpSize.Width)
                        {
                            nameGpSize = tempSize;
                        }
                    }
                    fontSize = fontSize -= 1f;
                    padFont = new Font(fontName, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                }
                while (cvSize < nameGpSize.Width);
                if (nameArr[1] == null)
                {
                    pointOrigin = new Point((800 - (int)nameGpSize.Width) / 2, (480 - (int)nameGpSize.Height) / 2);
                    graphicsPath.AddString(nameArr[0], padFont.FontFamily, (int)FontStyle.Bold, fontSize, pointOrigin, StringFormat.GenericTypographic);
                }
                else
                {
                    pointOrigin = new Point((800 - (int)nameGpSize.Width) / 2, (480/2) - (int)nameGpSize.Height);
                    graphicsPath.AddString(nameArr[0], padFont.FontFamily, (int)FontStyle.Bold, fontSize, pointOrigin, StringFormat.GenericTypographic);
                    pointOrigin = new Point((800 - (int)nameGpSize.Width) / 2, (480 / 2));
                    graphicsPath.AddString(nameArr[1], padFont.FontFamily, (int)FontStyle.Bold, fontSize, pointOrigin, StringFormat.GenericTypographic);

                }


                graphics.DrawPath(pen, graphicsPath);
                //graphics.DrawString(name, font, Brushes.Black, pointOrigin, StringFormat.GenericTypographic );

                //upper line
                fontSize = 30f;
                padFont = new Font("돋움체", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                pointOrigin = new Point(3, 10);

                if (name.Length > 18)
                {
                    name = name.Substring(0, 17);
                    name = name + "...";
                }

                graphics.DrawString(name, padFont, Brushes.Black, pointOrigin);
                SizeF tmpMeasure = graphics.MeasureString(birth, padFont, new PointF(0, 0), StringFormat.GenericTypographic);
                pointOrigin = new Point(780 - (int)tmpMeasure.Width, 10);

                graphics.DrawString(birth, padFont, Brushes.Black, pointOrigin);

                graphics.DrawLine(Pens.Red, new Point(5, 42), new Point(795, 42));

                /*byte[] byteArr = null;
                if (bitmap != null)
                {
                    MemoryStream stream = new MemoryStream();
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                    byteArr = stream.ToArray();
                }*/

                graphics.Dispose();
                padFont.Dispose();
            }



            m_bitmap = new Bitmap(m_capability.screenWidth, m_capability.screenHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            {
                Graphics gfx = Graphics.FromImage(m_bitmap);
                gfx.Clear(Color.White);
                gfx.DrawImage(bitmap, 0, 0, 800, 480);

                //Font font = new Font(FontFamily.GenericSansSerif, m_btns[0].Bounds.Height / 2F, GraphicsUnit.Pixel);
                Font font = new Font(FontFamily.GenericSansSerif, m_btns[0].Bounds.Height / 2F, FontStyle.Bold, GraphicsUnit.Pixel);

                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                if (useColor)
                {
                    gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                }
                else
                {
                    // Anti-aliasing should be turned off for monochrome devices.
                    gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
                }

                // Draw the buttons
                for (int i = 0; i < m_btns.Length; ++i)
                {
                    if (useColor)
                    {
                        if (i == 0)
                        gfx.FillRectangle(Brushes.Yellow, m_btns[i].Bounds);
                        else
                        gfx.FillRectangle(Brushes.LightGray, m_btns[i].Bounds);

                    }
                    gfx.DrawRectangle(Pens.Black, m_btns[i].Bounds);
                    gfx.DrawString(m_btns[i].Text, font, Brushes.Black, m_btns[i].Bounds, sf);
                }

                gfx.Dispose();
                font.Dispose();
            }
            {
                protocolHelper = new wgssSTU.ProtocolHelper();
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                m_bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                m_bitmapData = (byte[])protocolHelper.resizeAndFlatten(stream.ToArray(), 0, 0, (uint)m_bitmap.Width, (uint)m_bitmap.Height, m_capability.screenWidth, m_capability.screenHeight, (byte)m_encodingMode, wgssSTU.Scale.Scale_Fit, 0, 0);
                protocolHelper = null;
                stream.Dispose();
            }
            clearScreen();
            m_tablet.setInkingMode(0x01);
        }

        public void ResetPadImage()
        {
            signImage = new Bitmap(m_capability.screenWidth, m_capability.screenHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            {
                Graphics gfx = Graphics.FromImage(signImage);
                gfx.Clear(Color.White);
                gfx.Dispose();
            }
            MemoryStream memoryStream = new MemoryStream();
            signImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            byte[] imgBuf = memoryStream.ToArray();
            _ocrCamera.sign_img = Convert.ToBase64String(imgBuf);
        }

        private int getPenDataOptionMode()
        {
            int penDataOptionMode;

            try
            {
                penDataOptionMode = m_tablet.getPenDataOptionMode();
            }
            catch (Exception optionModeException)
            {
                //m_parent.print("Tablet doesn't support getPenDataOptionMode");
                penDataOptionMode = -1;
            }
            //m_parent.print("Pen data option mode: " + m_penDataOptionMode);

            return penDataOptionMode;
        }

        private void setPenDataOptionMode(int currentPenDataOptionMode)
        {
            // If the current option mode is TimeCount then this is a 520 so we must reset the mode
            // to basic data only as there is no handler for TimeCount

            //m_parent.print("current mode: " + currentPenDataOptionMode);

            switch (currentPenDataOptionMode)
            {
                case -1:
                    // THis must be the 300 which doesn't support getPenDataOptionMode at all so only basic data
                    m_penDataOptionMode = (int)PenDataOptionMode.PenDataOptionMode_None;
                    break;

                case (int)PenDataOptionMode.PenDataOptionMode_None:
                    // If the current option mode is "none" then it could be any pad so try setting the full option
                    // and if it fails or ends up as TimeCount then set it to none
                    try
                    {
                        m_tablet.setPenDataOptionMode((byte)wgssSTU.penDataOptionMode.PenDataOptionMode_TimeCountSequence);
                        m_penDataOptionMode = m_tablet.getPenDataOptionMode();
                        if (m_penDataOptionMode == (int)PenDataOptionMode.PenDataOptionMode_TimeCount)
                        {
                            m_tablet.setPenDataOptionMode((byte)wgssSTU.penDataOptionMode.PenDataOptionMode_None);
                            m_penDataOptionMode = (int)PenDataOptionMode.PenDataOptionMode_None;
                        }
                        else
                        {
                            m_penDataOptionMode = (int)PenDataOptionMode.PenDataOptionMode_TimeCountSequence;
                        }
                    }
                    catch (Exception ex)
                    {
                        // THis shouldn't happen but just in case...
                        //m_parent.print("Using basic pen data");
                        m_penDataOptionMode = (int)PenDataOptionMode.PenDataOptionMode_None;
                    }
                    break;

                case (int)PenDataOptionMode.PenDataOptionMode_TimeCount:
                    m_tablet.setPenDataOptionMode((byte)wgssSTU.penDataOptionMode.PenDataOptionMode_None);
                    m_penDataOptionMode = (int)PenDataOptionMode.PenDataOptionMode_None;
                    break;

                case (int)PenDataOptionMode.PenDataOptionMode_TimeCountSequence:
                    // If the current mode is timecountsequence then leave it at that
                    m_penDataOptionMode = currentPenDataOptionMode;
                    break;
            }

            switch ((int)m_penDataOptionMode)
            {
                case (int)PenDataOptionMode.PenDataOptionMode_None:
                    m_penData = new List<wgssSTU.IPenData>();
                    //m_parent.print("None");
                    break;
                case (int)PenDataOptionMode.PenDataOptionMode_TimeCount:
                    m_penData = new List<wgssSTU.IPenData>();
                    //m_parent.print("Time count");
                    break;
                case (int)PenDataOptionMode.PenDataOptionMode_SequenceNumber:
                    m_penData = new List<wgssSTU.IPenData>();
                    //m_parent.print("Seq number");
                    break;
                case (int)PenDataOptionMode.PenDataOptionMode_TimeCountSequence:
                    m_penTimeData = new List<wgssSTU.IPenDataTimeCountSequence>();
                    //m_parent.print("Time count + seq");
                    break;
                default:
                    m_penData = new List<wgssSTU.IPenData>();
                    break;
            }
        }

        private void addDelegates()
        {
            // Add the delegates that receive pen data.
            m_tablet.onGetReportException += new wgssSTU.ITabletEvents2_onGetReportExceptionEventHandler(onGetReportException);

            m_tablet.onPenData += new wgssSTU.ITabletEvents2_onPenDataEventHandler(onPenData);
            //m_tablet.onPenDataEncrypted += new wgssSTU.ITabletEvents2_onPenDataEncryptedEventHandler(onPenDataEncrypted);

            m_tablet.onPenDataTimeCountSequence += new wgssSTU.ITabletEvents2_onPenDataTimeCountSequenceEventHandler(onPenDataTimeCountSequence);
            //m_tablet.onPenDataTimeCountSequenceEncrypted += new wgssSTU.ITabletEvents2_onPenDataTimeCountSequenceEncryptedEventHandler(onPenDataTimeCountSequenceEncrypted);

        }

        private void onGetReportException(wgssSTU.ITabletEventsException tabletEventsException)
        {
            try
            {
                tabletEventsException.getException();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: " + e.Message);
                m_tablet.disconnect();
                m_tablet = null;
                m_penData = null;
                m_penTimeData = null;
            }
        }
        private int buttonClicked(Point pt)
        {
            int btn = 0; // will be +ve if the pen is over a button.
            {
                for (int i = 0; i < m_btns.Length; ++i)
                {
                    if (m_btns[i].Bounds.Contains(pt))
                    {
                        btn = i + 1;
                        //m_parent.print("Pressed button " + btn);
                        break;
                    }
                }
            }
            return btn;
        }

        private void onPenDataTimeCountSequence(wgssSTU.IPenDataTimeCountSequence penTimeData)
        {
            UInt16 penSequence;
            UInt16 penTimeStamp;
            UInt16 penPressure;
            UInt16 x;
            UInt16 y;

            penPressure = penTimeData.pressure;
            penTimeStamp = penTimeData.timeCount;
            penSequence = penTimeData.sequence;
            x = penTimeData.x;
            y = penTimeData.y;

            Point pt = tabletToScreen(penTimeData);
            int btn = buttonClicked(pt); // Check if a button was clicked

            bool isDown = (penTimeData.sw != 0);

            //m_parent.print("Handling pen data timed");

            // This code uses a model of four states the pen can be in:
            // down or up, and whether this is the first sample of that state.

            try
            {

                if (isDown)
                {
                    if (m_isDown == 0)
                    {
                        // transition to down
                        if (btn > 0)
                        {
                            // We have put the pen down on a button.
                            // Track the pen without inking on the client.

                            m_isDown = btn;
                        }
                        else
                        {
                            // We have put the pen down somewhere else.
                            // Treat it as part of the signature.

                            m_isDown = -1;
                        }
                    }
                    else
                    {
                        // already down, keep doing what we're doing!
                    }

                    // draw
                    if (m_penTimeData.Count != 0 && m_isDown == -1 && m_tablet.getInkingMode() == 0x01)
                    {
                        // Draw a line from the previous down point to this down point.
                        // This is the simplist thing you can do; a more sophisticated program
                        // can perform higher quality rendering than this!


                        //Graphics gfx = setQualityGraphics(this);
                        wgssSTU.IPenDataTimeCountSequence prevPenData = m_penTimeData[m_penTimeData.Count - 1];
                        //PointF prev = tabletToClient(prevPenData);

                        //gfx.DrawLine(m_penInk, prev, tabletToClientTimed(penTimeData));
                        //gfx.Dispose();

                        Graphics gfx = Graphics.FromImage(signImage);
                        PointF prev = tabletToScreen(prevPenData);
                        gfx.DrawLine(m_penInk, prev, tabletToScreen(penTimeData));
                        gfx.Dispose();

                        MemoryStream memoryStream = new MemoryStream();
                        signImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                        byte[] imgBuf = memoryStream.ToArray();


                        _ocrCamera.sign_img = Convert.ToBase64String(imgBuf);

                    }

                    // The pen is down, store it for use later.
                    if (m_isDown == -1)
                        m_penTimeData.Add(penTimeData);
                }
                else
                {
                    if (m_isDown != 0)
                    {
                        // transition to up
                        if (btn > 0)
                        {
                            // The pen is over a button

                            if (btn == m_isDown)
                            {
                                // The pen was pressed down over the same button as is was lifted now. 
                                // Consider that as a click!
                                //m_parent.print("Performing button " + btn);
                                m_btns[btn - 1].PerformClick();
                            }
                        }
                        m_isDown = 0;
                    }
                    else
                    {
                        // still up
                    }

                    // Add up data once we have collected some down data.
                    if (m_penTimeData != null)
                    {
                        if (m_penTimeData.Count != 0)
                            m_penTimeData.Add(penTimeData);
                    }
                }
            }
            catch (Exception)
            {
            }

        }

        private void onPenData(wgssSTU.IPenData penData) // Process incoming pen data
        {

            Point pt = tabletToScreen(penData);
            Console.WriteLine(penData.y);

            int btn = 0; // will be +ve if the pen is over a button.
            {
                for (int i = 0; i < m_btns.Length; ++i)
                {
                    if (m_btns[i].Bounds.Contains(pt))
                    {
                        btn = i + 1;
                        break;
                    }
                }
            }

            bool isDown = (penData.sw != 0);

            // This code uses a model of four states the pen can be in:
            // down or up, and whether this is the first sample of that state.

            if (isDown)
            {
                if (m_isDown == 0)
                {
                    // transition to down
                    if (btn > 0)
                    {
                        // We have put the pen down on a button.
                        // Track the pen without inking on the client.

                        m_isDown = btn;
                    }
                    else
                    {
                        // We have put the pen down somewhere else.
                        // Treat it as part of the signature.

                        m_isDown = -1;
                    }
                }
                else
                {
                    // already down, keep doing what we're doing!
                }

                // draw
                if (m_penData.Count != 0 && m_isDown == -1 && m_tablet.getInkingMode() == 0x01)
                {
                    // Draw a line from the previous down point to this down point.
                    // This is the simplist thing you can do; a more sophisticated program
                    // can perform higher quality rendering than this!

                    //Graphics gfx = this.CreateGraphics();
                    //gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    //gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    //gfx.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    //gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                    wgssSTU.IPenData prevPenData = m_penData[m_penData.Count - 1];

                    //PointF prev = tabletToClient(prevPenData);

                    //gfx.DrawLine(m_penInk, prev, tabletToClient(penData));
                    //gfx.Dispose();

                    Graphics gfx = Graphics.FromImage(signImage);
                    PointF prev = tabletToScreen(prevPenData);
                    gfx.DrawLine(m_penInk, prev, tabletToScreen(penData));
                    gfx.Dispose();

                    MemoryStream memoryStream = new MemoryStream();
                    signImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] imgBuf = memoryStream.ToArray();


                    _ocrCamera.sign_img = Convert.ToBase64String(imgBuf);
                }

                // The pen is down, store it for use later.
                if (m_isDown == -1)
                    m_penData.Add(penData);
            }
            else
            {
                if (m_isDown != 0)
                {
                    // transition to up
                    if (btn > 0)
                    {
                        // The pen is over a button

                        if (btn == m_isDown)
                        {
                            // The pen was pressed down over the same button as is was lifted now. 
                            // Consider that as a click!
                            m_btns[btn - 1].PerformClick();
                        }
                    }
                    m_isDown = 0;
                }
                else
                {
                    // still up
                }

                // Add up data once we have collected some down data.
                if (m_penData != null)
                {
                    if (m_penData.Count != 0)
                        m_penData.Add(penData);
                }
            }
        }

    }
}
