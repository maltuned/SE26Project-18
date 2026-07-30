// ==================== Re-exports from modules ====================
// All existing API functions are preserved with identical signatures.
// TanStack Query hooks are also exported for components that want caching.

// Config & fetch helpers
export { setAuthExpiredHandler, setLogoutInProgress } from "./fetch";

// Auth
export { login, register, getMe, changePassword } from "./modules/auth";

// User
export { getUserById, getUserProfile, getUsers, updateUser, updateUserSettings } from "./modules/user";

// Game
export { getGames, getGameById } from "./modules/game";

// Tags
export { getGameTags, getRecruitmentTags, initTagCaches } from "./modules/tag";

// Recruitment
export {
  getRecruitments,
  getRecruitmentsByGame,
  getRecruitmentById,
  getRecruitmentByChatId,
  getRecruitmentsByPublisherId,
  saveRecruitment,
  createRecruitment,
  updateRecruitment,
  deleteRecruitment,
} from "./modules/recruitment";

// Response
export {
  getResponses,
  getResponsesByUserId,
  createResponse,
  deleteResponse,
  updateResponseStatus,
} from "./modules/response";

// Chat
export {
  getChats,
  getChatById,
  getChatByUsers,
  getChatsByRecruitmentId,
  createChat,
  closeChat,
} from "./modules/chat";

// Message
export { getMessagesByChatId, sendMessage, markMessagesRead } from "./modules/message";

// Notification
export {
  getNotifications,
  getUnreadNotificationCount,
  markNotificationRead,
  markAllNotificationsRead,
} from "./modules/notification";

// Review
export { createReview, getReviewsByUser, hasReviewed } from "./modules/review";

// Feedback & Report
export { submitFeedback, submitReport } from "./modules/feedback";

// Image upload
export { uploadImage, uploadAvatar } from "./modules/image";

// TanStack Query provider
export { QueryProvider } from "./query-client";

// ==================== TanStack Query Hooks (new) ====================

export {
  useMeQuery,
  useLoginMutation,
  useRegisterMutation,
  useChangePasswordMutation,
} from "./modules/auth";

export {
  useUserQuery,
  useUserProfileQuery,
  useUsersQuery,
  useUpdateUserMutation,
  useUpdateUserSettingsMutation,
} from "./modules/user";

export { useGamesQuery, useGameQuery } from "./modules/game";
export { useGameTagsQuery, useRecruitmentTagsQuery } from "./modules/tag";

export {
  useRecruitmentsQuery,
  useRecruitmentsByGameQuery,
  useRecruitmentQuery,
  useRecruitmentByChatQuery,
  useRecruitmentsByPublisherQuery,
  useSaveRecruitmentMutation,
  useCreateRecruitmentMutation,
  useUpdateRecruitmentMutation,
  useDeleteRecruitmentMutation,
} from "./modules/recruitment";

export {
  useResponsesQuery,
  useResponsesByUserQuery,
  useCreateResponseMutation,
  useDeleteResponseMutation,
  useUpdateResponseStatusMutation,
} from "./modules/response";

export {
  useChatsQuery,
  useChatQuery,
  useChatsByRecruitmentQuery,
  useCreateChatMutation,
  useCloseChatMutation,
} from "./modules/chat";

export {
  useMessagesQuery,
  useSendMessageMutation,
  useMarkMessagesReadMutation,
} from "./modules/message";

export {
  useNotificationsQuery,
  useUnreadNotificationCountQuery,
  useMarkNotificationReadMutation,
  useMarkAllNotificationsReadMutation,
} from "./modules/notification";

export {
  useReviewsByUserQuery,
  useHasReviewedQuery,
  useCreateReviewMutation,
} from "./modules/review";

export { useSubmitFeedbackMutation, useSubmitReportMutation } from "./modules/feedback";

// ==================== Type re-exports ====================

// Re-export frontend data patterns for convenience
export type {
  ChatBrief, ChatData, ChatStatus, GameBrief, GameInfo, GameTag, MessageData, RecruitmentBrief, RecruitmentData,
  RecruitmentTag, ResponseData, ResponseStatus, UserInfo, UserSettings, ReviewData, NotificationItem
} from "./data-patterns";