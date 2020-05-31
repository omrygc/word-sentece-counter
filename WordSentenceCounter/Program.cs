using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WordSentenceCounter
{
    class Program
    {
        private static int ThreadCount = 5;
        private static int SentenceCount = 0;
        private static double AvgCount = 0;

        private static List<string> Sentences = new List<string>();
        private static Dictionary<string, int> WordCounts = new Dictionary<string, int>();
        private static Dictionary<int, ConcurrentQueue<string>> QUEUES = new Dictionary<int, ConcurrentQueue<string>>();

        private static ManualResetEvent mre = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            //Ana thread verilen dosyayı okuyup cümlelerine ayıracak , cümle sayısını ve tüm cümlelerdeki ortalama kelime sayısını tutacak.
            Task manager = new Task(WorkMainThread, TaskCreationOptions.LongRunning);
            manager.Start();
            Task.WaitAll(manager);

            Console.WriteLine();
            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        private static void WorkMainThread()
        {
            Console.WriteLine("Main thread is starting...");

            //string inputTxt = "Was certainty remaining engrossed applauded now sir how discovery. Settled opinion how enjoyed greater joy adapted too shy? Now properly surprise expenses interest nor replying she she . Bore sir tall nay many many time yet less.";           
            StreamReader sr = new StreamReader(@"C:\Users\Public\paragraf.txt");
            string inputTxt = sr.ReadToEnd();
            sr.Close();
            Sentences = inputTxt.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            AvgCount = Sentences.Average(x => x.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length);
            SentenceCount = Sentences.Count;

            //her thread e ait queue oluşturuldu.
            for (int i = 1; i <= ThreadCount; i++)
            {
                ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
                QUEUES.Add(i, queue);
            }

            //text içindeki her cümle sırayla thread lerin kuyruğuna eklendi.
            for (int i = 0; i <= SentenceCount - 1; i++)
            {
                int index = (i % ThreadCount) + 1;
                QUEUES[index].Enqueue(Sentences[i]);
            }

            Task[] childThreads = new Task[ThreadCount];

            for (int i = 0; i < ThreadCount; i++)
            {
                int threadId = i + 1;
                Task task = new Task(() => DoWork(threadId));
                childThreads[i] = task;
                task.Start();
            }

            //Ana thread tüm yardımcı threadlerin işlerini bitirmesini bekleyecek.            
            Task.WaitAll(childThreads);

            //cümle sayısını ,ortalama kelime sayısını ve tüm threadlerin oluşturdugu listeyi sayı ve kelimelerolarak ekrana yazdıracak
            Console.WriteLine();
            Console.WriteLine("Sentence Count : {0}", SentenceCount);
            Console.WriteLine("Avg.Word Count : {0}", AvgCount);
            Console.WriteLine();

            foreach (KeyValuePair<string, int> word in WordCounts.OrderByDescending(key => key.Value))
            {
                Console.WriteLine(word.Key + " - " + word.Value);
            }

            Console.WriteLine();
            Console.WriteLine("Main thread is stopping...");
        }

        private static void DoWork(int threadId)
        {
            Console.WriteLine("Thread {0} is starting.", threadId);
            string sentence;

            //thread kendine ait kuyruktan cümleleri alır.
            while (QUEUES[threadId].TryDequeue(out sentence))
            {
                //kuyruğa eklenen cümleleri kelimelerine ayırır.
                var words = sentence.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                //her kelime için global tutulan bir listede , ilgili kelime sayısı güncellenir.
                for (int i = 0; i < words.Count; i++)
                {
                    if (!WordCounts.ContainsKey(words[i]))
                    {
                        WordCounts.Add(words[i], 1);
                    }
                    else
                    {
                        WordCounts[words[i]] += 1;
                    }
                }

                Console.WriteLine("Thread {0} is processing item :  {1}", threadId, sentence);
            }

            Console.WriteLine("Thread {0} is stopping.", threadId);
        }
    }
}
