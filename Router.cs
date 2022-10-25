using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace BYOWebServer
{
    public class Router
    {


    public string WebSitePath { get; set; }

        private Dictionary<string, ExtensionInfo> extFolderMap;

        public Router()
        {
            extFolderMap = new Dictionary<string, ExtensionInfo>() 
            {
                { "ico",new ExtensionInfo() {Loader=ImageLoder,ContentType="image/ico" }},
                {"png", new ExtensionInfo(){ Loader = ImageLoader, ContentType="image/png"}},
                { "jpg", new ExtensionInfo() { Loader = ImageLoader, ContentType="image/jpg"}},
                {"gif", new ExtensionInfo() {Loader = ImageLoader, ContentType="image/gif"}},
                {"bmp", new ExtensionInfo(){ Loader = ImageLoader, ContentType="image/bmp"}},
                { "html", new ExtensionInfo(){ Loader = PageLoader, ContentType="text/html"}},
                {"css", new ExtensionInfo() {Loader = FileLoader,ContentType="file/css"}},
                {"js", new ExtensionInfo() { Loader = FileLoader, ContentType = "text/javascript"}},
                {"", new ExtensionInfo() { Loader = PageLoader, ContentType= "text/html"}}
           };
        }
     
       private ResponsePacket ImageLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            ResponsePacket response = new ResponsePacket() { Data = br.ReadBytes((int)fs.Length), ContentType = extInfo.ContentType };
            br.Close();
            fs.Close();

            return response;
        }

        private ResponsePacket FileLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            string text = File.ReadAllText(fullPath);
            ResponsePacket response = new ResponsePacket() { Data = Encoding.UTF8.GetBytes(text), ContentType = extInfo.ContentType, Encoding = Encoding.UTF8 };

            return response;
        }

        private ResponsePacket PageLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            ResponsePacket response = new ResponsePacket();
            
            if(fullPath == WebSitePath)
            {
                response = Route(GET, "index.html", null);
            }
            else
            {
                if(String.IsNullOrEmpty(ext))
                {
                    fullPath = fullPath + ".html";
                }

                fullPath = WebsitePath + "\\Pages" + fullPath.Substring(fullPath.IndexOf(WebSitePath));
                response = FileLoader(fullPath, ext, extInfo);
            }

            return response;
        }
        
        public ResponsePacket Route(string verb, string path, Dictionary<string, string> kvParams)
        {
            string ext = path.Substring(path.IndexOf(".") + 1);
            ExtensionInfo extInfo;
            ResponsePacket response = null; 

            if(extFolderMap.TryGetValue(ext, out extInfo))
            {
                string fullpath = Path.Combine(WebSitePath, path);
                response = extInfo.Loader(fullpath,ext,extInfo); 
            }
            return response;
        }

        public static void Respond(HttpListenerResponse response, ResponsePacket resp)
        {
            response.ContentType = resp.ContentType;
            response.ContentLength64 = resp.Data.Length;
            response.OutputStream.Write(resp.Data,0, resp.Data.Length);
            response.ContentEncoding = resp.Encoding;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.OutputStream.Close();
        }
   
    
    }

    public class ResponsePacket
    {
        public string Redirect { get;set;}
        public byte[] Data { get;set;}
        public string ContentType { get; set; }

        public Encoding Encoding { get; set; }
    }
    
    internal class ExtensionInfo
    {
        public string ContentType { get; set; }
        public Func<string, string, string, ExtensionInfo, ResponsePacket> Loader { get; set; } 
    }
    
    
}
