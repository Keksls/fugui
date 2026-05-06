// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define FUDEBUG
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using Fu.Framework;
using ImGuiNET;
#if FU_EXTERNALIZATION
using SDL2;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fu
{
    /// <summary>
    /// Fugui file and encoding helpers.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Read all bytes from a file, using UnityWebRequest on Android to support streaming assets, and File.ReadAllBytes on other platforms
        /// </summary>
        /// <param name="filePath"> path of the file to read</param>
        /// <returns> byte array of the file content, or null if an error occurs</returns>
        public static byte[] ReadAllBytes(string filePath)
        {
#if FUMOBILE
    using (var request = UnityEngine.Networking.UnityWebRequest.Get(filePath))
    {
        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
        }

        if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[FontLoader] Failed to load font from streaming assets: {filePath} - {request.error}");
            return null;
        }

        return request.downloadHandler.data;
    }
#else
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[FontLoader] Font file not found: {filePath}");
                return null;
            }

            return File.ReadAllBytes(filePath);
#endif
        }

        /// <summary>
        /// Read all text from a file, using UnityWebRequest on Android to support streaming assets, and File.ReadAllText on other platforms
        /// </summary>
        /// <param name="filePath"> path of the file to read</param>
        /// <returns> string of the file content, or null if an error occurs</returns>
        public static string ReadAllText(string filePath)
        {
            string text = Encoding.UTF8.GetString(ReadAllBytes(filePath));
            return text;
        }

        /// <summary>
        /// Convert a string to UTF8 byte array and return the number of bytes written to the array
        /// </summary>
        /// <param name="s"> string to convert</param>
        /// <param name="utf8Bytes"> pointer to the byte array that will receive the UTF8 bytes</param>
        /// <param name="utf8ByteCount"> size of the byte array</param>
        /// <returns> number of bytes written to the array</returns>
        public unsafe static int GetUtf8(string s, byte* utf8Bytes, int utf8ByteCount)
        {
            fixed (char* utf16Ptr = s)
            {
                return Encoding.UTF8.GetBytes(utf16Ptr, s.Length, utf8Bytes, utf8ByteCount);
            }
        }
    }
}
