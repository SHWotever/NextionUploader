namespace NextionUploader.Nextion
{
    public class NextionInfos
    {
        public NextionInfos()
        {
        }

        public NextionInfos(string response)
        {
            response = response.Substring(response.IndexOf(" ") + 1);

            var data = response.Split(',');
            this.Touch = data[0];
            this.Reserved = data[1];
            this.Model = data[2];
            this.FirmwareVersion = data[3];
            this.MCU = data[4];
            this.SeriaNumber = data[5];
            this.FLASHSize = data[6];
        }

        public string FirmwareVersion { get; set; }
        public string FLASHSize { get; set; }
        public string MCU { get; set; }
        public string Model { get; set; }
        public string Reserved { get; set; }
        public string SeriaNumber { get; set; }
        public string Touch { get; set; }
    }
}