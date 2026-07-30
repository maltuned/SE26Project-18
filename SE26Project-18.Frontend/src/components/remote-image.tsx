import { useEffect, useState } from "react";
import { ActivityIndicator, StyleSheet, View } from "react-native";
import { Image } from "expo-image";
import { useTheme } from "../contexts/theme-context";
import { tokenStorage } from "../api/token-storage";
import { API_BASE } from "../api/config";

const DEFAULT_AVATAR = require("../../assets/images/testImage.png");

interface RemoteImageProps {
  url?: string | null;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  style?: any;
  fallbackSource?: ReturnType<typeof require>;
}

function arrayBufferToDataUri(
  buffer: ArrayBuffer,
  contentType: string,
): string {
  const bytes = new Uint8Array(buffer);
  let binary = "";
  for (let i = 0; i < bytes.length; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  return `data:${contentType};base64,${btoa(binary)}`;
}

export default function RemoteImage({
  url,
  style,
  fallbackSource,
}: RemoteImageProps) {
  const { colors } = useTheme();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [imageUri, setImageUri] = useState<string | null>(null);

  useEffect(() => {
    if (!url) {
      setLoading(false);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(false);
    setImageUri(null);

    const loadImage = async () => {
      try {
        const token = await tokenStorage.getAccessToken();

        const resolvedUrl =
          url.startsWith("http://") || url.startsWith("https://")
            ? url
            : `${API_BASE}${url.startsWith("/") ? url : "/" + url}`;

        const xhr = new XMLHttpRequest();
        xhr.open("GET", resolvedUrl);
        xhr.responseType = "arraybuffer";
        if (token) {
          xhr.setRequestHeader("Authorization", `Bearer ${token}`);
        }

        xhr.onload = () => {
          if (cancelled) return;
          if (xhr.status === 200) {
            const contentType =
              xhr.getResponseHeader("content-type") || "image/jpeg";
            const dataUri = arrayBufferToDataUri(
              xhr.response as ArrayBuffer,
              contentType,
            );
            setImageUri(dataUri);
            setLoading(false);
          } else {
            setLoading(false);
            setError(true);
          }
        };
        xhr.onerror = () => {
          if (!cancelled) {
            setLoading(false);
            setError(true);
          }
        };
        xhr.send();
      } catch {
        if (!cancelled) {
          setLoading(false);
          setError(true);
        }
      }
    };

    loadImage();

    return () => {
      cancelled = true;
    };
  }, [url]);

  if (!url || error) {
    return <Image source={fallbackSource || DEFAULT_AVATAR} style={style} transition={140} />;
  }

  if (loading || !imageUri) {
    return (
      <View style={[style, { overflow: "hidden" }]}>
        <View
          style={[
            styles.loadingOverlay,
            { backgroundColor: colors.placeholder },
          ]}
        />
      </View>
    );
  }

  return <Image source={{ uri: imageUri }} style={style} transition={140} />;
}

const styles = StyleSheet.create({
  loadingOverlay: {
    ...StyleSheet.absoluteFill,
    justifyContent: "center",
    alignItems: "center",
  },
});
