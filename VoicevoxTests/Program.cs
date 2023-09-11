
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using VoicevoxCore;
using static VoicevoxCore.Voicevox;
using static VoicevoxCore.VoicevoxResultCode;

unsafe
{
    string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new();
    string jtalkPath = Path.Combine(assemblyFolder, "open_jtalk_dic_utf_8-1.11");
    Console.WriteLine($"jtalk loaded from \"{jtalkPath}\"");
    var initOptions = voicevox_make_default_initialize_options();
    Console.WriteLine("Initialization...");

    Console.Write("C# string: ");
    byte[] bytes2 = Encoding.UTF8.GetBytes(jtalkPath);
    byte* pointer = (byte*)Marshal.AllocHGlobal(bytes2.Length+1).ToPointer();
    for (int i = 0; i < bytes2.Length; i++)
    {
        pointer[i] = bytes2[i];
        Console.Write($"{bytes2[i]} ");
    }
    pointer[bytes2.Length] = 0;
    initOptions.load_all_models = true;
    initOptions.open_jtalk_dict_dir = (nint)pointer;
    Console.Write($"0");
    Console.WriteLine("\nC# ");
    Console.WriteLine($"struct: {(ulong)&initOptions}");
    Console.WriteLine($"acceleration_mode: {(ulong)&initOptions.acceleration_mode}, value: {*(uint*)&initOptions.acceleration_mode}");
    Console.WriteLine($"cpu_num_threads: {(ulong)&initOptions.cpu_num_threads}, value: {*(ushort*)&initOptions.cpu_num_threads}");
    Console.WriteLine($"load_all_models: {(ulong)&initOptions.load_all_models}");
    Console.WriteLine($"open_jtalk_dict_dir: {(ulong)&initOptions.open_jtalk_dict_dir}");

    for (var i = 0; i < sizeof(VoicevoxInitializeOptions); i++)
        Console.Write($"{((byte*)&initOptions)[i]}, ");
    Console.WriteLine() ;
    

    var resultCode = voicevox_initialize(initOptions);

    if (resultCode != VOICEVOX_RESULT_OK)
    {
        printErrorMessage(resultCode);
        return;
    }
    Console.WriteLine("Initialization sucsessfull!");
    Marshal.FreeHGlobal((nint)pointer);

    Console.WriteLine("Runnig TTS...");
    var ttsOptions = voicevox_make_default_tts_options();

    uint speakerId = 0;
    UIntPtr outputBinarySize = new UIntPtr(0);
    byte* outputWav = null;

    using (var text = new Utf8String(Console.ReadLine() ?? ""))
    {
        var result = voicevox_tts(text.IntPtr, speakerId, ttsOptions, ref outputBinarySize, ref outputWav);
        if (result != VOICEVOX_RESULT_OK)
        {
            printErrorMessage(result);
            return;
        }
    }

    byte[] bytes = new byte[((uint)outputBinarySize)];

    for (int i = 0; i < bytes.Length; i++)
    {
        bytes[i] = outputWav[i];
    }

    File.WriteAllBytes("output.wav", bytes);

    voicevox_wav_free(outputWav);

    voicevox_finalize();
}

unsafe void printErrorMessage(VoicevoxCore.VoicevoxResultCode resultCode)
{
    Console.WriteLine(voicevox_utils_pointer_to_string(voicevox_error_result_to_message(resultCode)));
}
internal unsafe class Utf8String : IDisposable
{
    IntPtr iPtr;
    public IntPtr IntPtr { get { return iPtr; } }
    public byte* pointer { get { return (byte*)iPtr.ToPointer(); } }
    public int BufferLength { get { return iBufferSize; } }
    int iBufferSize;
    public Utf8String(string aValue)
    {
        if (aValue == null)
        {
            iPtr = IntPtr.Zero;
        }
        else
        {
            byte[] bytes = Encoding.UTF8.GetBytes(aValue);
            Console.WriteLine();
            iPtr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, iPtr, bytes.Length);
            Marshal.WriteByte(iPtr, bytes.Length, 0);
            iBufferSize = bytes.Length + 1;
            Console.WriteLine();
        }
        Console.WriteLine($"STR: Init {(ulong)pointer}");
    }
    public void Dispose()
    {
        Console.WriteLine($"STR: Disposing {(ulong)pointer}");
        if (iPtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(iPtr);
            iPtr = IntPtr.Zero;
        }
    }
}