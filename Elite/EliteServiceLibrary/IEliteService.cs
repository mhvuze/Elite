/// <author>
/// Shawn Quereshi
/// </author>
namespace EliteServiceLibrary
{
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public interface IEliteService
    {
        [WebInvoke(UriTemplate = "SendKeyDown?virtualKey={virtualKey}?virtualKey={modKey}", Method = "POST")]
        [OperationContract(IsOneWay = true)]
        void SendKeyDown(ushort virtualKey, ushort modKey);

        [WebInvoke(UriTemplate = "SendKeyUp?virtualKey={virtualKey}?virtualKey={modKey}", Method = "POST")]
        [OperationContract(IsOneWay = true)]
        void SendKeyUp(ushort virtualKey, ushort modKey);
    }
}
