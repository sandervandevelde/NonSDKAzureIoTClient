namespace NonSDKAzureIoTClient
{
    internal class Topics
    {
        private string _clientId;
        public Topics(string clientId)
        {
            _clientId = clientId;
        }

        public string subscribe_topic_filter_cloudmessage => $"devices/{_clientId}/messages/devicebound/#";
        public string subscribe_topic_filter_directmethod => "$iothub/methods/POST/#";
        public string subscribe_topic_filter_desiredprop => "$iothub/twin/PATCH/properties/desired/#";
        public string subscribe_topic_filter_operationresponse => "$iothub/twin/res/#";
        public string send_message_topic => $"devices/{_clientId}/messages/events/";
        public string send_reported_properties_topic => "$iothub/twin/PATCH/properties/reported/?$rid=45";
        public string request_latest_device_twin_topic => "$iothub/twin/GET/?$rid=42";
        internal string send_direct_method_response_topic => "$iothub/methods/res/200/?$rid=";
    }
}
