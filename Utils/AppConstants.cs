using System;

namespace UpgradeApp.Utils
{
    /// <summary>
    /// Centralized constants for the WingetWizard application.
    /// Reduces string duplication and improves maintainability.
    /// </summary>
    public static class AppConstants
    {
        // Application Information
        public const string APP_NAME = "WingetWizard";
        public const string APP_DISPLAY_NAME = "🧿 WingetWizard";
        public const string APP_SUBTITLE = "AI-Enhanced Package Manager";
        public const string APP_VERSION = "v2.1";

        // Date/Time Formats
        public const string DATE_FORMAT_LOG = "yyyy-MM-dd HH:mm:ss.fff";
        public const string DATE_FORMAT_ISO = "yyyy-MM-ddTHH:mm:ss.fffZ";
        public const string DATE_FORMAT_FILE = "yyyyMMdd_HHmmss";
        public const string DATE_FORMAT_SHORT = "yyyy-MM-dd";
        public const string TIME_FORMAT = "HH:mm:ss.fff";

        // File Extensions
        public const string LOG_EXTENSION = ".log";
        public const string COMPRESSED_LOG_EXTENSION = ".log.gz";
        public const string CACHE_EXTENSION = ".cache";
        public const string MARKDOWN_EXTENSION = ".md";
        public const string JSON_EXTENSION = ".json";
        public const string CSV_EXTENSION = ".csv";
        public const string XML_EXTENSION = ".xml";

        // Directory Names
        public const string AI_REPORTS_DIR = "AI_Reports";
        public const string LOGS_DIR = "Logs";
        public const string CACHE_DIR = "Cache";
        public const string TEMP_DIR = "Temp";

        // File Names
        public const string SETTINGS_FILE = "settings.json";
        public const string CONFIG_FILE = "config.json";
        public const string CACHE_INDEX_FILE = "cache_index.json";

        // UI Messages
        public const string MSG_NO_SELECTION = "Please select packages to upgrade";
        public const string MSG_NO_PACKAGES = "No packages found";
        public const string MSG_OPERATION_COMPLETE = "Operation completed successfully";
        public const string MSG_OPERATION_FAILED = "Operation failed";
        public const string MSG_LOADING = "Loading...";
        public const string MSG_PROCESSING = "Processing...";

        // Status Messages
        public const string STATUS_READY = "Ready";
        public const string STATUS_CHECKING = "Checking for updates...";
        public const string STATUS_UPGRADING = "Upgrading packages...";
        public const string STATUS_INSTALLING = "Installing packages...";
        public const string STATUS_UNINSTALLING = "Uninstalling packages...";
        public const string STATUS_RESEARCHING = "AI Research in progress...";
        public const string STATUS_COMPLETE = "✅ Complete";
        public const string STATUS_FAILED = "❌ Failed";
        public const string STATUS_UPGRADED = "✅ Upgraded";
        public const string STATUS_VIEW_REPORT = "📄 View Report";

        // AI Providers
        public const string AI_PROVIDER_CLAUDE = "Claude";
        public const string AI_PROVIDER_PERPLEXITY = "Perplexity"; 
        public const string AI_PROVIDER_BEDROCK = "Bedrock";

        // Claude Models (Direct API)
        public const string AI_MODEL_CLAUDE_SONNET_4 = "claude-sonnet-4-20250514";
        public const string AI_MODEL_CLAUDE_3_5_SONNET = "claude-3-5-sonnet-20241022";
        public const string AI_MODEL_CLAUDE_3_5_HAIKU = "claude-3-5-haiku-20240307";

        // AWS Bedrock Models (2025 Updated)
        // Latest Claude Models
        public const string BEDROCK_CLAUDE_37_SONNET = "us.anthropic.claude-3-7-sonnet-20250219-v1:0";
        public const string BEDROCK_CLAUDE_SONNET_4 = "anthropic.claude-sonnet-4-20250115-v1:0";
        public const string BEDROCK_CLAUDE_OPUS_4 = "anthropic.claude-opus-4-1-20250805-v1:0";
        public const string BEDROCK_CLAUDE_35_SONNET_V2 = "anthropic.claude-3-5-sonnet-20241022-v2:0";
        
        // Stable Claude Models
        public const string BEDROCK_CLAUDE_35_SONNET = "anthropic.claude-3-5-sonnet-20240620-v1:0";
        public const string BEDROCK_CLAUDE_35_HAIKU = "anthropic.claude-3-5-haiku-20241022-v1:0";
        public const string BEDROCK_CLAUDE_3_OPUS = "anthropic.claude-3-opus-20240229-v1:0";
        
        // Meta Llama Models
        public const string BEDROCK_LLAMA_33_70B = "meta.llama3-3-70b-instruct-v1:0";
        public const string BEDROCK_LLAMA_32_90B = "meta.llama3-2-90b-instruct-v1:0";
        public const string BEDROCK_LLAMA_32_11B = "meta.llama3-2-11b-instruct-v1:0";
        public const string BEDROCK_LLAMA_31_405B = "meta.llama3-1-405b-instruct-v1:0";
        public const string BEDROCK_LLAMA_31_70B = "meta.llama3-1-70b-instruct-v1:0";
        public const string BEDROCK_LLAMA_31_8B = "meta.llama3-1-8b-instruct-v1:0";
        
        // Amazon Titan Models
        public const string BEDROCK_TITAN_TEXT_PREMIER = "amazon.titan-text-premier-v1:0";
        public const string BEDROCK_TITAN_TEXT_EXPRESS = "amazon.titan-text-express-v1";
        public const string BEDROCK_TITAN_EMBED_TEXT = "amazon.titan-embed-text-v1";

        // API Endpoints
        public const string CLAUDE_API_URL = "https://api.anthropic.com";
        public const string PERPLEXITY_API_URL = "https://api.perplexity.ai";

        // AWS Configuration
        public const string DEFAULT_AWS_REGION = "us-east-1";
        public const string BEDROCK_SERVICE_NAME = "bedrock-runtime";

        // Performance Thresholds
        public const int DEFAULT_PAGE_SIZE = 50;
        public const int VIRTUALIZATION_THRESHOLD = 100;
        public const int DEBOUNCE_DELAY_MS = 300;
        public const int NETWORK_TIMEOUT_MS = 5000;
        public const long MEMORY_WARNING_THRESHOLD_MB = 500;
        public const double CPU_WARNING_THRESHOLD_PERCENT = 80.0;
        public const long OPERATION_WARNING_THRESHOLD_MS = 5000;

        // Validation Limits
        public const int MAX_PACKAGE_NAME_LENGTH = 100;
        public const int MAX_VERSION_LENGTH = 50;
        public const int MAX_API_KEY_LENGTH = 200;
        public const int MAX_FILE_NAME_LENGTH = 255;
        public const int MAX_DESCRIPTION_LENGTH = 1000;
        public const int MAX_PATH_LENGTH = 260;

        // Cache Configuration
        public const int DEFAULT_MAX_MEMORY_ENTRIES = 1000;
        public const int DEFAULT_MAX_DISK_ENTRIES = 10000;
        public const int DEFAULT_CACHE_EXPIRATION_HOURS = 24;
        public const int CACHE_CLEANUP_INTERVAL_MS = 300000; // 5 minutes

        // Logging Configuration
        public const int DEFAULT_LOG_BUFFER_SIZE = 1000;
        public const int DEFAULT_LOG_FILE_SIZE_MB = 10;
        public const int DEFAULT_MAX_LOG_FILES = 30;
        public const int LOG_ROTATION_INTERVAL_MS = 86400000; // 24 hours

        // Health Check Thresholds
        public const long MIN_FREE_DISK_SPACE_MB = 100;
        public const long MAX_MEMORY_USAGE_MB = 500;
        public const int MAX_TEMP_FILES = 100;
        public const int MAX_THREAD_COUNT = 50;

        // Regex Patterns (as constants for reuse)
        public const string PACKAGE_NAME_PATTERN = @"^[a-zA-Z0-9._\-]+$";
        public const string VERSION_PATTERN = @"^[0-9]+\.[0-9]+(\.[0-9]+)?(\.[0-9]+)?$";
        public const string API_KEY_PATTERN = @"^[a-zA-Z0-9\-_]{20,}$";
        public const string FILE_NAME_PATTERN = @"^[a-zA-Z0-9._\- ]+$";
        public const string URL_PATTERN = @"^https?://[^\s/$.?#].[^\s]*$";

        // UI Colors (as hex strings for consistency)
        public const string PRIMARY_BLUE_HEX = "#3B82F6";
        public const string SUCCESS_GREEN_HEX = "#10B981";
        public const string WARNING_YELLOW_HEX = "#F59E0B";
        public const string ERROR_RED_HEX = "#EF4444";
        public const string DARK_BACKGROUND_HEX = "#0F0F0F";
        public const string LIGHT_BACKGROUND_HEX = "#F0F0F0";

        // Font Information
        public const string PRIMARY_FONT_FAMILY = "Segoe UI";
        public const string FALLBACK_FONT_FAMILY = "Calibri";
        public const string MONOSPACE_FONT_FAMILY = "Consolas";
        public const float DEFAULT_FONT_SIZE = 11F;
        public const float HEADER_FONT_SIZE = 18F;
        public const float BUTTON_FONT_SIZE = 10F;

        // Winget Commands
        public const string WINGET_LIST = "list";
        public const string WINGET_UPGRADE = "upgrade";
        public const string WINGET_INSTALL = "install";
        public const string WINGET_UNINSTALL = "uninstall";
        public const string WINGET_SEARCH = "search";
        public const string WINGET_SOURCE = "source";

        // Winget Sources
        public const string SOURCE_WINGET = "winget";
        public const string SOURCE_MSSTORE = "msstore";
        public const string SOURCE_ALL = "all";

        // Error Messages
        public const string ERROR_INVALID_INPUT = "Invalid input provided";
        public const string ERROR_FILE_NOT_FOUND = "File not found";
        public const string ERROR_NETWORK_ERROR = "Network error occurred";
        public const string ERROR_API_KEY_INVALID = "Invalid API key";
        public const string ERROR_PERMISSION_DENIED = "Permission denied";
        public const string ERROR_DISK_SPACE_LOW = "Insufficient disk space";
        public const string ERROR_MEMORY_LIMIT_EXCEEDED = "Memory limit exceeded";

        // Success Messages
        public const string SUCCESS_OPERATION_COMPLETE = "Operation completed successfully";
        public const string SUCCESS_FILE_SAVED = "File saved successfully";
        public const string SUCCESS_CACHE_CLEARED = "Cache cleared successfully";
        public const string SUCCESS_SETTINGS_SAVED = "Settings saved successfully";

        // Tooltips
        public const string TOOLTIP_CHECK_UPDATES = "Check for available package updates";
        public const string TOOLTIP_UPGRADE_SELECTED = "Upgrade selected packages";
        public const string TOOLTIP_UPGRADE_ALL = "Upgrade all available packages";
        public const string TOOLTIP_AI_RESEARCH = "Get AI-powered upgrade recommendations";
        public const string TOOLTIP_EXPORT_DATA = "Export package list and reports";
        public const string TOOLTIP_VIEW_LOGS = "Show/hide detailed operation logs";
        public const string TOOLTIP_SETTINGS = "Configure application settings";

        // Help Text
        public const string HELP_GETTING_STARTED = "Getting Started with WingetWizard";
        public const string HELP_AI_FEATURES = "AI-Powered Package Analysis";
        public const string HELP_ADVANCED_FEATURES = "Advanced Features and Settings";
        public const string HELP_TROUBLESHOOTING = "Troubleshooting Common Issues";

        // Validation Error Messages
        public const string VALIDATION_PACKAGE_NAME_EMPTY = "Package name cannot be empty";
        public const string VALIDATION_VERSION_EMPTY = "Version cannot be empty";
        public const string VALIDATION_API_KEY_EMPTY = "API key cannot be empty";
        public const string VALIDATION_FILE_NAME_EMPTY = "File name cannot be empty";
        public const string VALIDATION_INVALID_FORMAT = "Invalid format";
        public const string VALIDATION_LENGTH_EXCEEDED = "Maximum length exceeded";
        public const string VALIDATION_DANGEROUS_PATTERN = "Contains potentially dangerous patterns";

        // Security Messages
        public const string SECURITY_API_KEY_CONFIGURED = "Sensitive setting configured. Ensure proper security measures.";
        public const string SECURITY_PERMISSION_CHECK_FAILED = "Permission validation failed";
        public const string SECURITY_DANGEROUS_OPERATION = "Potentially dangerous operation detected";

        // Performance Messages
        public const string PERF_HIGH_MEMORY_USAGE = "High memory usage detected";
        public const string PERF_HIGH_CPU_USAGE = "High CPU usage detected";
        public const string PERF_SLOW_OPERATION = "Slow operation detected";
        public const string PERF_CACHE_MISS = "Cache miss occurred";

        // Logging Categories
        public const string LOG_CATEGORY_MAIN = "MainForm";
        public const string LOG_CATEGORY_PACKAGE = "PackageService";
        public const string LOG_CATEGORY_AI = "AIService";
        public const string LOG_CATEGORY_SETTINGS = "SettingsService";
        public const string LOG_CATEGORY_HEALTH = "HealthCheck";
        public const string LOG_CATEGORY_PERFORMANCE = "Performance";
        public const string LOG_CATEGORY_CACHE = "Cache";
        public const string LOG_CATEGORY_VALIDATION = "Validation";
        public const string LOG_CATEGORY_SECURITY = "Security";

        // File Names
        public const string LOG_FILE_PREFIX = "WingetWizard";
        public const string REPORTS_DIRECTORY = "AI_Reports";
        public const string LOGS_DIRECTORY = "Logs";

        // Feature Flags (for future use)
        public const bool ENABLE_ADVANCED_LOGGING = true;
        public const bool ENABLE_PERFORMANCE_MONITORING = true;
        public const bool ENABLE_HEALTH_CHECKS = true;
        public const bool ENABLE_CACHING = true;
        public const bool ENABLE_VIRTUALIZATION = true;
        public const bool ENABLE_DEBOUNCED_SEARCH = true;
    }
}

