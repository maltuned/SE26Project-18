import { ApiResponse } from "../dtos";
import { tokenStorage } from "../token-storage";
import { API_BASE } from "../config";

export const uploadImage = async (uri: string, folder: string = "avatars"): Promise<string | null> => {
  return new Promise(async (resolve) => {
    const formData = new FormData();
    const filename = uri.split("/").pop() || "image.jpg";
    formData.append("file", {
      uri,
      name: filename,
      type: "image/jpeg",
    } as any);
    formData.append("folder", folder);

    const token = await tokenStorage.getAccessToken();
    const xhr = new XMLHttpRequest();
    xhr.open("POST", `${API_BASE}/Image/upload`);

    if (token) {
      xhr.setRequestHeader("Authorization", `Bearer ${token}`);
    }

    xhr.onload = () => {
      if (xhr.status === 200) {
        try {
          const data: ApiResponse<string> = JSON.parse(xhr.responseText);
          resolve(data.status === 200 ? data.data : null);
        } catch {
          resolve(null);
        }
      } else {
        resolve(null);
      }
    };
    xhr.onerror = () => resolve(null);
    xhr.send(formData);
  });
};

export const uploadAvatar = async (uri: string, userId: number): Promise<string | null> => {
  return new Promise(async (resolve) => {
    const formData = new FormData();
    const filename = uri.split("/").pop() || "avatar.jpg";
    formData.append("file", {
      uri,
      name: filename,
      type: "image/jpeg",
    } as any);
    formData.append("userId", String(userId));

    const token = await tokenStorage.getAccessToken();
    const xhr = new XMLHttpRequest();
    xhr.open("POST", `${API_BASE}/Image/upload-avatar`);

    if (token) {
      xhr.setRequestHeader("Authorization", `Bearer ${token}`);
    }

    xhr.onload = () => {
      if (xhr.status === 200) {
        try {
          const data: ApiResponse<string> = JSON.parse(xhr.responseText);
          resolve(data.status === 200 ? data.data : null);
        } catch {
          resolve(null);
        }
      } else {
        resolve(null);
      }
    };
    xhr.onerror = () => resolve(null);
    xhr.send(formData);
  });
};