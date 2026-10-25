# Casa Clara — frontend

Aplicación Vue 3 + TypeScript para las finanzas familiares. Usa Composition API, Pinia, Vue Router, Axios y Tailwind CSS.

## Desarrollo

1. Inicia la API ASP.NET Core en HTTPS.
2. Copia `.env.example` a `.env.local` y configura `VITE_PROXY_TARGET` con el origen HTTPS de la API (por ejemplo, `https://localhost:7001`).
3. Ejecuta:

   ```powershell
   npm.cmd install
   npm.cmd run dev
   ```

La aplicación estará disponible en `http://127.0.0.1:5173`. Vite reenvía `/api` a la API para evitar configurar CORS durante el desarrollo. La API debe admitir cookies seguras en `localhost` para renovar la sesión.

## Verificación y build

```powershell
npm.cmd test
npm.cmd run typecheck
npm.cmd run build
```

Las pruebas Vitest cubren autenticación, registro de gastos, presupuestos y carga/revisión OCR.

En producción, sirve el contenido de `dist/` y configura el servidor web para que las rutas `/api` apunten a la API. `VITE_API_BASE_URL` permite establecer una ruta base distinta durante la compilación.

## Vistas

- Inicio de sesión y registro
- Resumen financiero del hogar
- Gastos e ingresos
- Presupuestos por mes
- Carga y revisión de resultados OCR para imagen/PDF
- Perfil, hogar y categorías
