import tensorflow as tf
import tf2onnx
import onnx

# Крок 1: Завантаження вашої моделі
model = tf.keras.models.load_model("models\model.h5")

# Крок 2: Визначення сигнатури входу
# Тут: [None, 32, 32, 1] — як у вашій функції detect_sign
spec = (tf.TensorSpec((None, 32, 32, 1), tf.float32, name="input"),)

# Крок 3: Конвертація в ONNX
onnx_model, _ = tf2onnx.convert.from_keras(model, input_signature=spec, opset=13)

# Крок 4: Збереження ONNX моделі
onnx.save_model(onnx_model, "model.onnx")

print("✅ Модель успішно збережена як model.onnx")