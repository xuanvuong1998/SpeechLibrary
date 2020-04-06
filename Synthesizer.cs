using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace SpeechLibrary
{
    public class Synthesizer
    {
        private static SpeechSynthesizer synthesizer = new SpeechSynthesizer();        

        private static CultureInfo chineseCulture, englishCulture;
        private static bool isEnglishMode;

        /// <summary>        
        /// </summary>
        /// <returns>The list of all installed voices name (Local)</returns>
        public static List<string> GetInstalledVoicesName()
        {
            List<string> res = new List<string>();

            foreach (var item in synthesizer.GetInstalledVoices())
            {
                res.Add(item.VoiceInfo.Name);
            }

            return res;
        }
        
        static Synthesizer()
        {
            Setup();
        }
        private static void Setup()
        {
            isEnglishMode = true; // English is defaut           
            synthesizer.Rate = 0; // Default          
            foreach (var item in synthesizer.GetInstalledVoices())
            {
                if (item.VoiceInfo.Culture.Name == "en-US")
                {
                    englishCulture = item.VoiceInfo.Culture;
                }
                else if (item.VoiceInfo.Culture.Name == "zh-CN")
                {
                    chineseCulture = item.VoiceInfo.Culture;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="language">en: english, ch: chinese</param>
        public static void SetLanguage(string language)
        {
            if (language == "en")
            {
                if (!isEnglishMode)
                {
                    isEnglishMode = true;
                    synthesizer.SelectVoiceByHints
                        (synthesizer.Voice.Gender, synthesizer.Voice.Age, 1, englishCulture);
                }
            }else if (language == "ch")
            {
                if (isEnglishMode)
                {
                    synthesizer.SelectVoiceByHints
                        (synthesizer.Voice.Gender, synthesizer.Voice.Age, 1, chineseCulture);
                    isEnglishMode = false;
                }
            }
        }
        
        /// <summary>
        /// Set volume of speaker (1 - 10)
        /// </summary>
        /// <param name="vol">min 1: max: 10</param>
        public static void SetVolume(int vol)
        {
            synthesizer.Volume = vol;
        }

        /// <summary>
        /// Set speed or rate of the voice (Min 0, Max 10)
        /// </summary>
        /// <param name="rate"></param>
        public static void SetSpeed(int rate)
        {
            synthesizer.Rate = rate;
        }
        /// <summary>
        /// Set age for the voice if available (dowload different voices in https://harposoftware.com/) 
        /// </summary>
        /// 
        /// <param name="age">child, teen, adult</param>
        public static void SetAge(string age)
        {
            age = age.ToLower();
            if (age == "teen")
            {
                synthesizer.SelectVoiceByHints(synthesizer.Voice.Gender, VoiceAge.Teen);
            }
            else if (age == "child")
            {
                synthesizer.SelectVoiceByHints(synthesizer.Voice.Gender, VoiceAge.Child);
            }
            else
            {
                synthesizer.SelectVoiceByHints(synthesizer.Voice.Gender, VoiceAge.Adult);
            }
        }
        /// <summary>
        /// Set gender of the voice 
        /// </summary>
        /// <param name="gender">male, female</param>
        public static void SetGender(string gender)
        {
            gender = gender.ToLower();
            if (gender == "female")
            {
                synthesizer.SelectVoiceByHints(VoiceGender.Female, synthesizer.Voice.Age);
            }
            else
            {
                synthesizer.SelectVoiceByHints(VoiceGender.Male, synthesizer.Voice.Age);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gender">male, female</param>
        /// <param name="age">child, teen, adult</param>
        public static void SelectVoiceByGenderAndAge(string gender, string age)
        {
            if (gender != null) SetGender(gender);
            if (age != null) SetAge(age);
        }

        /// <summary>
        /// Select voice by name which must installed on the system. See installed voice by calling 
        /// GetInstalledVoices(). If the chosen voice is not available, voice will be set as default
        /// </summary>
        /// <example>Vocalizer Expressive Tian-tian Harpo 22kHz, IVONA 2 Ivy OEM</example>
        /// <param name="voiceName"></param>
        public static void SelectVoiceByName(string voiceName)
        {
            try
            {
                synthesizer.SelectVoice(voiceName);
            }
            catch
            {
                
            }
        }

        /// <summary>
        /// Pause the current speak
        /// </summary>  
        public static void Pause()
        {
            synthesizer.Pause();
        }

        /// <summary>
        /// Continue speaking        
        /// </summary>
        public static void Resume()
        {
            if (synthesizer.State == SynthesizerState.Paused)
                synthesizer.Resume();
        }

        /// <summary>
        /// Speak a message synchronously
        /// </summary>
        /// <param name="message"></param>
        public static void Speak(string message)
        {
            try
            {
                synthesizer.Speak("");
                synthesizer.Speak(message);
            }
            catch
            {

            }
        }
        /// <summary>
        /// Speak a message asynchronously
        /// </summary>
        /// <param name="message"></param>
        public static void SpeakAsync(string message)
        {
            try
            {
                synthesizer.Speak("");
                synthesizer.SpeakAsync(message);
            }
            catch
            {

            }

        }
        
        /// <summary>
        /// Stop speaking everything. This can throw an exception though it can make everything stop speaking
        /// </summary>
        public static void StopSpeaking()
        {
            try
            {
                synthesizer.SpeakAsyncCancelAll();
            }
            catch { }
        }
    }
}
