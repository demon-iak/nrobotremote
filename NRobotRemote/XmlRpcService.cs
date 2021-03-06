﻿using System;
using System.IO;
using System.Net;﻿  ﻿
using CookComputing.XmlRpc;
using System.Reflection; 
using System.Xml;﻿  ﻿ 
using System.Xml.XPath; 
using System.Threading; 
using System.Diagnostics;
using log4net;
using NRobotRemote.Domain;

namespace NRobotRemote
{

	/// <summary>
﻿  	/// Class of XML-RPC methods for remote library (server)
﻿  	/// that conforms to RobotFramework remote library API
﻿  	/// </summary>
﻿ 	public class XmlRpcService : XmlRpcListenerService, IRemoteService
﻿  	{
﻿  ﻿  	
		//log4net
		private static readonly ILog log = LogManager.GetLogger(typeof(XmlRpcService));
		
		//properties
		private const String CStopRemoteServer = "STOP REMOTE SERVER";
		private const String CIntro = "__INTRO__";
		private const String CInit = "__INIT__";
		private RemoteService _service;
		private KeywordMap _map;
		
		
﻿  ﻿  	//constructor﻿
		public XmlRpcService(RemoteService service) 
		{
			if (service==null) throw new Exception("No service specified to XmlRpcService constructor");
			_service = service;
		}

﻿  ﻿  
	﻿  ﻿  /// <summary>
	﻿  ﻿  /// Get a list of keywords available for use
	﻿  ﻿  /// </summary>
	﻿  ﻿  public string[] get_keyword_names()
	  ﻿  ﻿{
	﻿  ﻿  ﻿	try 
			{
				log.Debug("XmlRpc Method call - get_keyword_names");
				var kwnames =  _map.GetKeywordNames();
				kwnames.Add(CStopRemoteServer);
				var result = kwnames.ToArray();
				log.Debug(String.Format("Keyword names are {0}",String.Join(",",result)));
				return result;
			}
			catch (Exception e)
			{
				log.Error(String.Format("Exception in method - get_keyword_names : {0}",e.Message));
				throw new XmlRpcFaultException(1,e.Message);
			}
	﻿  ﻿  }
﻿  ﻿  
	﻿  ﻿  /// <summary>
	﻿  ﻿  /// Run specified Robot Framework keyword from remote server.
	﻿  ﻿  /// </summary>
	﻿  ﻿  public XmlRpcStruct run_keyword(string keyword, object[] args)
	  ﻿  ﻿{
			log.Debug(String.Format("XmlRpc Method call - run_keyword {0}",keyword));
			XmlRpcStruct kr = new XmlRpcStruct();
			//check for stop remote server
			if (String.Equals(keyword,CStopRemoteServer,StringComparison.CurrentCultureIgnoreCase))
			{
				//start background thread to raise event
				Thread stopthread = new Thread(delayed_stop_remote_server);
				stopthread.IsBackground = true;
				stopthread.Start();
				log.Debug("Stop remote server thread started");
				//return success
				kr.Add("return",String.Empty);
				kr.Add("status","PASS");
				kr.Add("error",String.Empty);
				kr.Add("traceback",String.Empty);
				kr.Add("output",String.Empty);
			}
			else
			{
				try
				{
					var result = _map.ExecuteKeyword(keyword,args);
					log.Debug(result.ToString());
					kr = XmlRpcResultBuilder.ToXmlRpcResult(result);
				}
				catch (Exception e)
				{
					log.Error(String.Format("Exception in method - run_keyword : {0}",e.Message));
					throw new XmlRpcFaultException(1,e.Message);
				}
			}
			return kr;
		}
		
		/// <summary>
	﻿  ﻿  /// Get list of arguments for specified Robot Framework keyword.
	﻿  ﻿  /// </summary>
	﻿  ﻿  public string[] get_keyword_arguments(string keyword)
	﻿  ﻿  {
			log.Debug(String.Format("XmlRpc Method call - get_keyword_arguments {0}",keyword));
			if (String.Equals(keyword,CStopRemoteServer,StringComparison.CurrentCultureIgnoreCase))
			{
				return null;
			}
			try
			{
				var names = _map.GetKeywordArguments(keyword);
				log.Debug(String.Format("Keyword arguments are, {0}",String.Join(",",names)));
				return names;
			}
			catch (Exception e)
			{
				log.Error(String.Format("Exception in method - get_keyword_arguments : {0}",e.Message));
				throw new XmlRpcFaultException(1,e.Message);
			}
		
			
	﻿  ﻿  }﻿  ﻿  
			
	﻿  ﻿  /// <summary>
	﻿  ﻿  /// Get documentation for specified Robot Framework keyword.
	﻿  ﻿  /// Done by reading the .NET compiler generated XML documentation
	﻿  ﻿  /// for the loaded class library.
	﻿  ﻿  /// </summary>
	﻿  ﻿  /// <param name="keyword">The keyword to get documentation for.</param>
	﻿  ﻿  /// <returns>A documentation string for the given keyword.</returns>
	﻿  ﻿  public string get_keyword_documentation(string keyword)
	﻿  ﻿  {
		﻿  ﻿ 	log.Debug(String.Format("XmlRpc Method call - get_keyword_documentation {0}",keyword));
			//check for stop_remote_server
			if (String.Equals(keyword,CStopRemoteServer,StringComparison.CurrentCultureIgnoreCase))
			{
				return "Raises event to stop the remote server in the server host";
			}
			try
			{
				//check for INTRO 
				if (String.Equals(keyword,CIntro,StringComparison.CurrentCultureIgnoreCase))
				{
					var libdoc = _map._doc;
					log.Debug(String.Format("Library documentation, {0}",libdoc));
					return libdoc;
					
				}
				//check for init
				if (String.Equals(keyword,CInit,StringComparison.CurrentCultureIgnoreCase))
				{
					return String.Empty;    
				}
				//get keyword documentation
				var doc =  _map.GetKeywordDoc(keyword);
				log.Debug(String.Format("Keyword documentation, {0}",doc));
				return doc;
			}
			catch (Exception e)
			{
				log.Error(String.Format("Exception in method - get_keyword_documentation : {0}",e.Message));
				throw new XmlRpcFaultException(1,e.Message);
			}
				
	﻿  ﻿  }

	﻿  	/// <summary>
		/// Raises event in RemoteService that a stop request was received
		/// </summary>
		public void stop_remote_server()
		{
			log.Debug("XmlRpc Method call - stop_remote_server");
			_service.OnStopRequested(EventArgs.Empty);
		}
	
		/// <summary>
		/// If stop remote server executed as keyword, need to delay to allow return value
		/// </summary>
		public void delayed_stop_remote_server()
		{
			System.Threading.Thread.Sleep(4000);
			stop_remote_server();
		}
		
		/// <summary>
		/// Process xmlrpc request with a specific keyword map
		/// </summary>
		public void ProcessRequest(HttpListenerContext RequestContext, KeywordMap map)
		{
			if (map==null) throw new Exception("No keyword map for xmlrpc process");
			_map = map;
			base.ProcessRequest(RequestContext);
		}
	
}

}