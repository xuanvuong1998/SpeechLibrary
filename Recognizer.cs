using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.CognitiveServices.Speech;
using Timer = System.Timers.Timer;

namespace SpeechLibrary
{
    public class Recognizer
    {
        private static SpeechConfig config;
        private static string[] keywords;
        private static bool IsRecognizing { get; set; }                
        private static bool IsRecognizingKeyword { get; set; }     
        /// <summary>
        /// The property to indicate the required keyword is recognized or not yet
        /// </summary>
        public static bool KeywordRecognized { get; set; }

        private static int minRequiredQueryLeng;

        /// <summary>
        /// set or get the final recognized words
        /// </summary>
        public static string RecognizedWords { get; set; }
        
        private static int _silenceRecognizedCount;

        private static readonly int MAX_SILENCE_COUNT = 5;

        private static Timer checkingSilenceTimer = new Timer();
        
        /// <summary>
        /// The interval to check the silence sound. Default value is 1 second         
        /// </summary>
        public static int SilenceTimeOut { get; set; }

        private static int currentRecognizedWordsCount, latestRecognizedWordsCount;
        
        private static void StartCheckingSilence()
        {
            Debug.WriteLine("Start Checking slience");            
            currentRecognizedWordsCount = 0;
            latestRecognizedWordsCount = 0;            
            checkingSilenceTimer.Start();           
        }     

        private static void StopCheckingSilence()
        {
            checkingSilenceTimer.Stop();
        }
       
        private static void InitTimers()
        {
            checkingSilenceTimer.Interval = SilenceTimeOut == 0 ? 1000 : SilenceTimeOut;
            checkingSilenceTimer.AutoReset = true;
            Debug.WriteLine("Init Successfully!");
            checkingSilenceTimer.Elapsed += checkingSilenceTimer_Elapsed;
        }

        /// <summary>        
        /// </summary>
        /// <param name="subscriptionKey">Visit azure portal to have the subscription key</param>
        /// <param name="subcriptionRegion">Visit azure portal to have the subcription region</param>
        public static void Setup(string subscriptionKey, string subcriptionRegion)
        {
            config = SpeechConfig.FromSubscription(subscriptionKey, subcriptionRegion);

            InitTimers();
            
        }

        private static void checkingSilenceTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Debug.WriteLine("Checking : " + latestRecognizedWordsCount + " <> " + currentRecognizedWordsCount);
            if (IsQueryAcceptable(RecognizedWords) &&
                latestRecognizedWordsCount == currentRecognizedWordsCount)                
            {
                Debug.WriteLine("NO MORE WORDS");
                IsRecognizing = false;
                StopCheckingSilence();
            }
            else
            {
                latestRecognizedWordsCount = currentRecognizedWordsCount;
            }
        }
        /// <summary>        
        /// </summary>
        /// <param name="config">SpeechConfig including speech subcription and subscription region</param>
        public static void Setup(SpeechConfig config)
        {
            Recognizer.config = config;            
            InitTimers();
        }

        private static void ResetBeforeRecognizing()
        {
            IsRecognizing = true;
            RecognizedWords = "";
            KeywordRecognized = false;
            _silenceRecognizedCount = 0;
        }

        private static void ResetAfterTerminating()
        {
            RecognizedWords = "";
            KeywordRecognized = false;            
        }

        private static async Task StartRecognizing(SpeechRecognizer recog)
        {
            ResetBeforeRecognizing();         
            await recog.StartContinuousRecognitionAsync().ConfigureAwait(false);
            while (IsRecognizing) ;
        }


        /// <summary>
        /// Force stop recognizing but record recognized words
        /// </summary>
        public static void ForceStopRecognizing()
        {
            IsRecognizing = false;
        }

        /// <summary>
        /// Stop recognizing immediately without getting any recognized ulternances
        /// </summary>
        public static void StopAndDeleteRecognizedWords()
        {
            ForceStopRecognizing();
            ResetAfterTerminating();
        }
        private static async Task StopRecognizing(SpeechRecognizer recog)
        {
            Debug.WriteLine("STOP RECOGNIZING !!!");            
            await recog.StopContinuousRecognitionAsync().ConfigureAwait(false);
        }


        /// <summary>
        /// Recognize 
        /// </summary>
        /// <param name="requiredKeywords"></param>
        /// <returns></returns>        
        public static async Task<string> RecognizeKeyword(string requiredKeywords)
        {

            keywords = requiredKeywords.Split('|');
            IsRecognizingKeyword = true;            
            using (var recognizer = new SpeechRecognizer(config))
            {
                recognizer.Recognized += Recognizer_Recognized;
                recognizer.Recognizing += Recognizer_Recognizing;
                await StartRecognizing(recognizer);
                await StopRecognizing(recognizer);

                return RecognizedWords;
            }
        }

        /// <summary>
        /// Recognize keyword with timeout
        /// </summary>
        /// <param name="requiredKeywords">required keywords</param>
        /// <param name="maxRecogTime">After this period of time, stop recognizing</param>
        /// <returns></returns>
        public static async Task<string> RecognizeKeywordWithTimeout(string requiredKeywords, int maxRecogTime)
        {
            using (var timer = new Timer())
            {
                timer.Interval = maxRecogTime * 1000;
                timer.AutoReset = false;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
                return await RecognizeKeyword(requiredKeywords);                                    
            }
        }
        /// <summary>
        /// Recognize a single query (ulterance) until meet a first silence sound
        /// If after 5 times recognized empty string, the recognizer will stop
        /// </summary>
        /// <returns></returns>
        public static async Task<string> RecognizeQuery(int minRequiredLength)
        {
            IsRecognizingKeyword = false;
            minRequiredQueryLeng = minRequiredLength;
            using (var recognizer = new SpeechRecognizer(config))
            {
                recognizer.Recognized += Recognizer_Recognized;
                recognizer.Recognizing += Recognizer_Recognizing;
                StartCheckingSilence();
                await StartRecognizing(recognizer);
                await StopRecognizing(recognizer);
                StopCheckingSilence();
                return RecognizedWords;
            }
        }
        
        /// <summary>
        /// Recognize question with a specific timeout (It may stop before timeout when 
        /// recognzied the proper query and meet a silence sound
        /// </summary>
        /// <param name="minRequiredLength"></param>
        /// <param name="maxRecogTime"></param>
        /// <returns></returns>
        public static async Task<string> RecognizeQueryWithTimeOut(int minRequiredLength, int maxRecogTime)
        {
            using (var timer = new Timer())
            {
                timer.Interval = maxRecogTime * 1000;
                timer.AutoReset = false;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
               
                return await RecognizeQuery(minRequiredLength);
            }            
        }

        private static bool IsQueryAcceptable(string query)
        {
            return query.Length >= minRequiredQueryLeng;
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Debug.WriteLine("Recognizing timeout");
            IsRecognizing = false;
        }

        private static void CheckKeywords(string recogWords)
        {
            if (IsRecognizing == false) return;
            recogWords = recogWords.ToLower();            
            foreach (var item in keywords)
            {
                //Debug.WriteLine("Checking : " + recogWords + " --- " + item);
                if (recogWords.Contains(item))
                {                    
                    KeywordRecognized = true;
                    IsRecognizing = false;
                    return;
                }
            }            
        }

        private static void Recognizer_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            if (IsRecognizing == false) return;            
            RecognizedWords = e.Result.Text;
            Debug.WriteLine("Recognizing: " + RecognizedWords);
            if (IsRecognizingKeyword)
            {
                CheckKeywords(RecognizedWords);
            }
            else
            {
                currentRecognizedWordsCount++;
            }
        }

        private static void Recognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            if (IsRecognizing == false) return;
            Debug.WriteLine("Recognized: " + e.Result.Text + " -- " + RecognizedWords);
            if (IsRecognizingKeyword)
            {
                CheckKeywords(e.Result.Text);
            }
            else
            {
                if (IsQueryAcceptable(e.Result.Text))
                {                    
                    RecognizedWords = e.Result.Text;
                    IsRecognizing = false;
                }
                else
                {                    
                    if (string.IsNullOrWhiteSpace(e.Result.Text))
                    {
                        _silenceRecognizedCount++;                        
                    }

                    if (_silenceRecognizedCount >= MAX_SILENCE_COUNT)
                    {
                        Debug.WriteLine("SILENCE DETECTED 5 TIMES");
                        IsRecognizing = false;
                    }
                    else if (IsQueryAcceptable(RecognizedWords))
                    {
                        IsRecognizing = false;
                    }
                }
            }
        }
    }

    internal class DirectLineClient
    {
        private string directLineSecret;
        private string botId;
        private static string fromUser = "User";
        private static string id = "default-user";
        private Conversation conversation;
        Microsoft.Bot.Connector.DirectLine.DirectLineClient client = null;
        string watermark = null;


        public DirectLineClient(string secret, string id)
        {
            this.directLineSecret = secret;
            this.botId = id;
        }


        public void Initialize()
        {
            // connect to directline
            client = new Microsoft.Bot.Connector.DirectLine.DirectLineClient(directLineSecret);            
            //if next line shows error, it means no internet / poor connection at start up.
            conversation = client.Conversations.StartConversation();
        }

        public async Task<IEnumerable<Activity>> ReadBotMessagesAsyncDriver()
        {
            return await ReadBotMessagesAsync(client, conversation.ConversationId);
        }

        private async Task<IEnumerable<Activity>> ReadBotMessagesAsync(Microsoft.Bot.Connector.DirectLine.DirectLineClient client, string conversationId)
        {

            var activitySet = await client.Conversations.GetActivitiesAsync(conversationId, watermark).ConfigureAwait(false);
            watermark = activitySet.Watermark;
            var activities = from x in activitySet.Activities
                             where x.From.Id == botId
                             select x;
            return activities;


        }

        public async Task PostQuestionToBotAsync(string input)
        {
            Activity userMsg = new Activity
            {
                From = new ChannelAccount(id, fromUser),
                Speak = input,
                Text = input,
                Type = ActivityTypes.Message,
                TextFormat = "plain"
            };

            //send question to base to display 
            await client.Conversations.PostActivityAsync(this.conversation.ConversationId, userMsg);
        }
       
    }

    public class ChatBot
    {
        private static DirectLineClient directLineClient;        
       
        /// <summary>        
        /// </summary>
        /// <param name="directLineKey">Key to link between application and chatbot through direct line
        /// Can get this key from azure (chat bot -> channel)
        /// </param>
        /// <param name="botId">Name of the chat bot, get from azure service</param>
        public static void Setup(string directLineKey, string botId)
        {
            directLineClient = new DirectLineClient(directLineKey, botId);
            directLineClient.Initialize();

            Debug.WriteLine("Hand Shake");
            Task.Factory.StartNew(() => HandShake().ConfigureAwait(false));            
        }
        
        private static async Task HandShake()
        {
            await GetResponse("hello. This is a handshake").ConfigureAwait(false);
        }

        private static string GetRandomResponse(string[] allAnswers)
        {
            Random rand = new Random();
            string randomAns = allAnswers[rand.Next(allAnswers.Length)];

            return randomAns;
        }

        /// <summary>
        /// Get a random Response from the QnA MAKER (Multiple answers with delimiter '|')
        /// </summary>
        /// <param name="question"></param>
        /// <returns></returns>
        public static async Task<string> GetResponse(string question)
        {
            Debug.WriteLine("Getting response");
            await directLineClient.PostQuestionToBotAsync(question).ConfigureAwait(false);
            IEnumerable<Activity> activities = await directLineClient.ReadBotMessagesAsyncDriver().ConfigureAwait(false);
            Debug.WriteLine("Got the response!");
            foreach (Activity activity in activities)
            {
                if (activity.Text != null)
                {
                    string[] allAnswers = activity.Text.Split('|');

                    string randomAns = GetRandomResponse(allAnswers);
                    Debug.WriteLine("Response: " + randomAns);
                    return randomAns;
                }
            }
            return null;
        }
    }
}
