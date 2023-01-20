namespace CD1HW.Model
{
    public class OcrMsg
    {
        public OcrResult ocr_result { get; set; }
        public ResultImg result_image { get; set; }
    }
    // orc result 4 demo paramater
    public class OcrResult
    {
        public string ocr_text { get; set; }
        // id card
        public string id_card_type { get; set; }
        public string name { get; set; }
        public string regnum { get; set; }
        public string birth { get; set; }
        public string driver_num { get; set; }
        public string issue_date { get; set; }
        public string addr { get; set; }
        // cradit card
        public string cradit_card_num { get; set; }
        public string expiry_date { get; set; }
        // biz license
        public string biz_lic_type { get; set; }
        public string company_name { get; set; }
        public string biz_lic_num { get; set; }
        public string corp_reg_num { get; set; }
        //recepit
        public string shop_name { get; set; }
        public string approval_num { get; set; }
        public string trading_date { get; set; }
        public string total_amount { get; set; }
    }
    public class ResultImg
    {
        public string name_img { get; set; }
        public string regnum_img { get; set; }
        public string face_img { get; set; }
        public string birth_img { get; set; }

    }
}
