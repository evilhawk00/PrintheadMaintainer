/*
* 
* Copyright (C) 2021  YAN-LIN, CHEN
* 
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <https://www.gnu.org/licenses/>.
* 
*/
using PrintheadMaintainerUI.GlobalConstants;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace PrintheadMaintainerUI.NamedPipeClient
{
    public static class IPCClient
    {
        public static async Task AsyncSendMsgToServerAndDecodeResponse(string strExpectedRequestID, string strFullMsg, Action<int> CallbackMethodToExecute) {

            string strResponse = await Task.Run(() => {

                return StrAsyncSendMsgToServerAndGetResponse(strFullMsg);
                

            });

            if (strResponse != null)
            {
                int intServerResult = IntMessageDecoder(strExpectedRequestID, strResponse);

                if (CallbackMethodToExecute != null)
                {
                    CallbackMethodToExecute(intServerResult);
                }
            }
            else {
                //server response is null, just treat as failed
                if (CallbackMethodToExecute != null)
                {
                    CallbackMethodToExecute(NamedPipeConstants.intServerResult_Fail);
                }
            }


        }

        public static int IntSyncSendMsgToServerAndDecodeResponse(string strExpectedRequestID, string strFullMsg)
        {

            string strResponse = StrSyncSendMsgToServerAndGetResponse(strFullMsg);

            if (strResponse == null)
            {
                return NamedPipeConstants.intServerResult_Fail;
            }

            int intServerResult = IntMessageDecoder(strExpectedRequestID, strResponse);

            return intServerResult;

        }


        public static async Task<string> StrAsyncSendMsgToServerAndGetResponse(string strMsg)
        {
            // Create Named Pipes
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", NamedPipeConstants.NamedPipeName, PipeDirection.InOut))
            {

                

                var AsyncResult = await Task.Run(() => {

                    // Connect Named Pipes
                    try
                    {
                        pipeClient.Connect(NamedPipeConstants.intConnectionTimeout);
                    }
                    catch (Exception)
                    {
                        return null;
                    }

                    pipeClient.ReadMode = PipeTransmissionMode.Message;

                    byte[] messageBytes = Encoding.Unicode.GetBytes(strMsg); // for c++ wstring UTF-16

                    // Send the message to the server
                    pipeClient.Write(messageBytes, 0, messageBytes.Length);

                    return ReadMessage(pipeClient);
                });

                if (AsyncResult != null)
                {
                    string strServerResponse = Encoding.Unicode.GetString(AsyncResult);
                    return strServerResponse;
                }
                else {
                    return null;
                }
                
                

            }

        }

        public static string StrSyncSendMsgToServerAndGetResponse(string strMsg)
        {
            // Create Named Pipes
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", NamedPipeConstants.NamedPipeName, PipeDirection.InOut))
            {

                // Connect Named Pipes
                try
                {
                    pipeClient.Connect(NamedPipeConstants.intConnectionTimeout);
                }
                catch (Exception)
                {
                    return null;
                }

                pipeClient.ReadMode = PipeTransmissionMode.Message;

                byte[] messageBytes = Encoding.Unicode.GetBytes(strMsg); // for c++ wstring UTF-16

                // Send the message to the server
                pipeClient.Write(messageBytes, 0, messageBytes.Length);

                var result =  ReadMessage(pipeClient);
                

                if (result != null)
                {
                    string strServerResponse = Encoding.Unicode.GetString(result);
                    return strServerResponse;
                }
                else
                {
                    return null;
                }



            }

        }


        public static string StrMessageEncoder(string strRequestID, string strJobFlag, string strValueData) {

            //0~7 byte : Msgid
            //8~11 byte : action to do
            //12~ : valuedata, can be null

            if (strRequestID == null || strJobFlag == null) {

                return null;
            }

            //we need to make sure the string is null terminated for c++ server

            if (strValueData != null) {
                return strRequestID + strJobFlag + strValueData + "\0";
            }
            else{
                return strRequestID + strJobFlag + "\0";
            }
        
        }

        public static int IntMessageDecoder(string strExpectedRequestID, string strFullMsg) {

            if (strExpectedRequestID == null || strFullMsg == null) {
                return NamedPipeConstants.intServerResult_Fail;
            }

            string strRequestID;
            string strResultPart;



            try {
                strRequestID = strFullMsg.Substring(0, 8);
                strResultPart = strFullMsg.Substring(8,4);
            }
            catch (ArgumentOutOfRangeException) { 
            
                return NamedPipeConstants.intServerResult_Fail; ;
            }

            if (strExpectedRequestID.Equals(strRequestID)) {

                if (strResultPart.Equals(NamedPipeConstants.Reply_OperationSuccess))
                {
                    return NamedPipeConstants.intServerResult_SUCCESS;
                }
                else if (strResultPart.Equals(NamedPipeConstants.Reply_OperationFail))
                {
                    return NamedPipeConstants.intServerResult_Fail;
                }
                else if (strResultPart.Equals(NamedPipeConstants.Reply_StatusPrintingNow))
                {
                    return NamedPipeConstants.intServerResult_StatusPrintingNow;
                }
                else if (strResultPart.Equals(NamedPipeConstants.Reply_StatusNotPrinting))
                {
                    return NamedPipeConstants.intServerResult_StatusNotPrinting;
                }
                else if (strResultPart.Equals(NamedPipeConstants.Reply_StatusWaitingCountdownBeforePrint))
                {
                    return NamedPipeConstants.intServerResult_StatusWaitingCountdownBeforePrint;
                }
                else if (strResultPart.Equals(NamedPipeConstants.Reply_StatusScheduledPrintingFailedRecordUpdated))
                {
                    return NamedPipeConstants.intServerResult_StatusScheduledPrintingFailedRecordUpdated;
                }
                else if (strResultPart.Equals(NamedPipeConstants.Reply_StatusManualPrintingFailedRecordUpdated))
                {
                    return NamedPipeConstants.intServerResult_StatusManualPrintingFailedRecordUpdated;
                }
                else if (strResultPart.Equals(NamedPipeConstants.Reply_StatusPrintingSucceededRecently))
                {
                    return NamedPipeConstants.intServerResult_StatusPrintingSucceededRecently;
                }
                else if (strResultPart.Equals(NamedPipeConstants.Reply_IllegalMsgReceived))
                {
                    return NamedPipeConstants.intServerResult_IllegalMsgReceived;
                }
                else
                {
                    return NamedPipeConstants.intServerResult_Fail;
                }

            }
            else{

                return NamedPipeConstants.intServerResult_Fail;

            }

        }

        private static byte[] ReadMessage(PipeStream pipe)
        {
            byte[] buffer = new byte[1024];
            using (var ms = new MemoryStream())
            {
                do
                {
                    var readBytes = pipe.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, readBytes);
                }
                while (!pipe.IsMessageComplete);

                return ms.ToArray();
            }
        }

    }
}
