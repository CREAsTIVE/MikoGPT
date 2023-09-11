using System;
using System.Runtime.InteropServices;

namespace VoicevoxCore
{
    public enum VoicevoxAccelerationMode : int
    {
        VOICEVOX_ACCELERATION_MODE_AUTO = 0,
        VOICEVOX_ACCELERATION_MODE_CPU = 1,
        VOICEVOX_ACCELERATION_MODE_GPU = 2
    }

    public enum VoicevoxResultCode : int
    {
        VOICEVOX_RESULT_OK = 0,
        VOICEVOX_RESULT_NOT_LOADED_OPENJTALK_DICT_ERROR = 1,
        VOICEVOX_RESULT_LOAD_MODEL_ERROR = 2,
        VOICEVOX_RESULT_GET_SUPPORTED_DEVICES_ERROR = 3,
        VOICEVOX_RESULT_GPU_SUPPORT_ERROR = 4,
        VOICEVOX_RESULT_LOAD_METAS_ERROR = 5,
        VOICEVOX_RESULT_UNINITIALIZED_STATUS_ERROR = 6,
        VOICEVOX_RESULT_INVALID_SPEAKER_ID_ERROR = 7,
        VOICEVOX_RESULT_INVALID_MODEL_INDEX_ERROR = 8,
        VOICEVOX_RESULT_INFERENCE_ERROR = 9,
        VOICEVOX_RESULT_EXTRACT_FULL_CONTEXT_LABEL_ERROR = 10,
        VOICEVOX_RESULT_INVALID_UTF8_INPUT_ERROR = 11,
        VOICEVOX_RESULT_PARSE_KANA_ERROR = 12,
        VOICEVOX_RESULT_INVALID_AUDIO_QUERY_ERROR = 13
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VoicevoxInitializeOptions
    {
        public VoicevoxAccelerationMode acceleration_mode;
        public ushort cpu_num_threads;
        public bool load_all_models;
        public IntPtr open_jtalk_dict_dir;
    }

    public struct VoicevoxAudioQueryOptions
    {
        public bool kana;
    }

    public struct VoicevoxSynthesisOptions
    {
        public bool enable_interrogative_upspeak;
    }

    public struct VoicevoxTtsOptions
    {
        public bool kana;
        public bool enable_interrogative_upspeak;
    }

    public static class Voicevox
    {
        [DllImport("voicevox_core", EntryPoint = "19")]
        public static extern VoicevoxInitializeOptions voicevox_make_default_initialize_options();

        [DllImport("voicevox_core")]
        public static extern VoicevoxResultCode voicevox_initialize(VoicevoxInitializeOptions options);

        [DllImport("voicevox_core")]
        public static extern IntPtr voicevox_get_version();

        [DllImport("voicevox_core")]
        public static extern VoicevoxResultCode voicevox_load_model(uint speaker_id);

        [DllImport("voicevox_core")]
        public static extern bool voicevox_is_gpu_mode();

        [DllImport("voicevox_core")]
        public static extern bool voicevox_is_model_loaded(uint speaker_id);

        [DllImport("voicevox_core")]
        public static extern void voicevox_finalize();

        [DllImport("voicevox_core")]
        public static extern IntPtr voicevox_get_metas_json();

        [DllImport("voicevox_core")]
        public static extern IntPtr voicevox_get_supported_devices_json();

        [DllImport("voicevox_core")]
        public static extern VoicevoxResultCode voicevox_predict_duration(
            UIntPtr length,
            long[] phoneme_vector,
            uint speaker_id,
            ref UIntPtr output_predict_duration_data_length,
            out IntPtr output_predict_duration_data);

        [DllImport("voicevox_core")]
        public static extern void voicevox_predict_duration_data_free(IntPtr predict_duration_data);

        [DllImport("voicevox_core")]
        public static extern VoicevoxResultCode voicevox_predict_intonation(
            UIntPtr length,
            long[] vowel_phoneme_vector,
            long[] consonant_phoneme_vector,
            long[] start_accent_vector,
            long[] end_accent_vector,
            long[] start_accent_phrase_vector,
            long[] end_accent_phrase_vector,
            uint speaker_id,
            ref UIntPtr output_predict_intonation_data_length,
            out IntPtr output_predict_intonation_data);

        [DllImport("voicevox_core")]
        public static extern void voicevox_predict_intonation_data_free(IntPtr predict_intonation_data);

        [DllImport("voicevox_core")]
        public static extern VoicevoxResultCode voicevox_decode(
            UIntPtr length,
            UIntPtr phoneme_size,
            float[] f0,
            float[] phoneme_vector,
            uint speaker_id,
            ref UIntPtr output_decode_data_length,
            out IntPtr output_decode_data);

        [DllImport("voicevox_core")]
        public static extern void voicevox_decode_data_free(IntPtr decode_data);

        [DllImport("voicevox_core")]
        public static extern VoicevoxAudioQueryOptions voicevox_make_default_audio_query_options();

        [DllImport("voicevox_core")]
        public static extern VoicevoxResultCode voicevox_audio_query(
            string text,
            uint speaker_id,
            VoicevoxAudioQueryOptions options,
            out IntPtr output_audio_query_json);

        [DllImport("voicevox_core")]
        public static extern VoicevoxSynthesisOptions voicevox_make_default_synthesis_options();

        [DllImport("voicevox_core")]
        public static extern VoicevoxResultCode voicevox_synthesis(
            string audio_query_json,
            uint speaker_id,
            VoicevoxSynthesisOptions options,
            ref UIntPtr output_wav_length,
            out IntPtr output_wav);

        [DllImport("voicevox_core")]
        public static extern VoicevoxTtsOptions voicevox_make_default_tts_options();

        [DllImport("voicevox_core")]
        public static unsafe extern VoicevoxResultCode voicevox_tts(
            IntPtr text,
            uint speaker_id,
            VoicevoxTtsOptions options,
            ref UIntPtr output_wav_length,
            ref byte* output_wav);

        [DllImport("voicevox_core")]
        public static extern void voicevox_audio_query_json_free(IntPtr audio_query_json);

        [DllImport("voicevox_core")]
        public static unsafe extern void voicevox_wav_free(byte* wav);

        [DllImport("voicevox_core")]
        public static unsafe extern char* voicevox_error_result_to_message(VoicevoxResultCode result_code);
        public static unsafe string voicevox_utils_pointer_to_string(char* pointer)
        {
            return Marshal.PtrToStringAnsi((IntPtr)pointer) ?? throw new ArgumentException("wrong pointer");
        }
    }
}