import { useMutation } from "@tanstack/react-query";
import { FeedbackDto, ReportDto } from "../dtos";
import { apiPost, handlePostResponse } from "../fetch";

export const submitFeedback = async (data: FeedbackDto): Promise<boolean> => {
  const response = apiPost<boolean>("/Feedback", data);
  return handlePostResponse(response, (d: boolean) => d);
};

export const submitReport = async (data: ReportDto): Promise<boolean> => {
  const response = apiPost<boolean>("/Report", data);
  return handlePostResponse(response, (d: boolean) => d);
};

// ==================== TanStack Query Hooks ====================

export function useSubmitFeedbackMutation() {
  return useMutation({
    mutationFn: submitFeedback,
  });
}

export function useSubmitReportMutation() {
  return useMutation({
    mutationFn: submitReport,
  });
}