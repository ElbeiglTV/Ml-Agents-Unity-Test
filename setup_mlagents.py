#!/usr/bin/env python3
"""
Script para arreglar automáticamente la instalación de ML-Agents
Soluciona problemas de protobuf, versiones de Python y dependencias
"""

import subprocess
import sys
import os
import platform
from pathlib import Path

def run_command(command, description):
    """Ejecuta un comando y muestra el resultado"""
    print(f"\n🔄 {description}...")
    print(f"Comando: {command}")
    
    try:
        result = subprocess.run(command, shell=True, capture_output=True, text=True)
        if result.returncode == 0:
            print(f"✅ {description} completado exitosamente")
            if result.stdout.strip():
                print(f"Salida: {result.stdout.strip()}")
        else:
            print(f"❌ Error en {description}")
            print(f"Error: {result.stderr.strip()}")
            return False
    except Exception as e:
        print(f"❌ Excepción en {description}: {e}")
        return False
    
    return True

def check_python_version():
    """Verifica la versión de Python"""
    version = sys.version_info
    print(f"\n🐍 Versión de Python: {version.major}.{version.minor}.{version.micro}")
    
    if version.major == 3 and version.minor >= 13:
        print("⚠️  Python 3.13+ detectado. ML-Agents puede tener problemas.")
        print("   Recomendado: Python 3.10-3.11")
        return "python313"
    elif version.major == 3 and version.minor >= 10:
        print("✅ Versión de Python compatible")
        return "compatible"
    else:
        print("❌ Versión de Python muy antigua")
        return "incompatible"

def get_mlagents_version(python_type):
    """Obtiene la versión de ML-Agents compatible"""
    if python_type == "python313":
        # Para Python 3.13, intentar con versiones más recientes
        return "1.1.0"
    elif python_type == "compatible":
        # Para Python 3.10-3.11, usar la versión más reciente compatible
        return "1.1.0"
    else:
        return "1.1.0"

def uninstall_packages():
    """Desinstala paquetes problemáticos"""
    packages_to_remove = [
        "mlagents",
        "mlagents-envs", 
        "protobuf",
        "torch",
        "torchvision"
    ]
    
    for package in packages_to_remove:
        run_command(f"pip uninstall {package} -y", f"Desinstalando {package}")

def install_correct_versions(python_type):
    """Instala las versiones correctas de los paquetes"""
    print("\n📦 Instalando versiones compatibles...")
    
    # Instalar protobuf compatible primero
    if not run_command("pip install protobuf==3.20.3", "Instalando protobuf 3.20.3"):
        return False
    
    # Obtener versión de ML-Agents compatible
    mlagents_version = get_mlagents_version(python_type)
    print(f"📋 Instalando ML-Agents {mlagents_version} para Python {sys.version_info.major}.{sys.version_info.minor}")
    
    # Instalar ML-Agents
    if not run_command(f"pip install mlagents=={mlagents_version}", f"Instalando ML-Agents {mlagents_version}"):
        # Si falla, intentar sin versión específica
        print("🔄 Intentando instalar la última versión compatible...")
        if not run_command("pip install mlagents", "Instalando ML-Agents (última versión)"):
            return False
    
    return True

def test_installation():
    """Prueba la instalación de ML-Agents"""
    print("\n🧪 Probando instalación...")
    
    test_code = """
import sys
try:
    import mlagents
    import mlagents_envs
    print(f"✅ ML-Agents instalado correctamente")
    print(f"   Versión: {mlagents.__version__}")
    print(f"   Python: {sys.version}")
    return True
except ImportError as e:
    print(f"❌ Error de importación: {e}")
    return False
except Exception as e:
    print(f"❌ Error inesperado: {e}")
    return False
"""
    
    result = subprocess.run([sys.executable, "-c", test_code], 
                          capture_output=True, text=True)
    
    print(result.stdout)
    if result.stderr:
        print(f"Errores: {result.stderr}")
    
    return result.returncode == 0

def create_training_script():
    """Crea un script de entrenamiento"""
    training_script = '''#!/usr/bin/env python3
"""
Script de entrenamiento para ML-Agents Navigation
"""

import os
import subprocess
import sys

def main():
    # Configurar variable de entorno para evitar problemas de protobuf
    os.environ["PROTOCOL_BUFFERS_PYTHON_IMPLEMENTATION"] = "python"
    
    # Comando de entrenamiento
    config_path = "Assets/ML-Agents/Configs/nav_config.yaml"
    run_id = "nav_run_001"
    
    command = f"mlagents-learn {config_path} --run-id={run_id} --time-scale=20 --no-graphics"
    
    print("🚀 Iniciando entrenamiento de ML-Agents...")
    print(f"Comando: {command}")
    print("\\n💡 Recuerda:")
    print("   1. Abrir Unity y la escena Navigation3D")
    print("   2. Pulsar Play en Unity")
    print("   3. Para parar: Ctrl+C en esta terminal y detener Play en Unity")
    print("\\n" + "="*50)
    
    try:
        subprocess.run(command, shell=True)
    except KeyboardInterrupt:
        print("\\n⏹️  Entrenamiento detenido por el usuario")
    except Exception as e:
        print(f"\\n❌ Error durante el entrenamiento: {e}")

if __name__ == "__main__":
    main()
'''
    
    with open("train_navigation.py", "w", encoding="utf-8") as f:
        f.write(training_script)
    
    print("✅ Script de entrenamiento creado: train_navigation.py")

def suggest_python_installation():
    """Sugiere instalar Python 3.11"""
    print("\n💡 RECOMENDACIÓN: Instalar Python 3.11")
    print("=" * 50)
    print("Para mejor compatibilidad con ML-Agents:")
    print("1. Descarga Python 3.11 desde: https://www.python.org/downloads/")
    print("2. Instala Python 3.11 (marca 'Add to PATH')")
    print("3. Crea un entorno virtual:")
    print("   python3.11 -m venv mlagents_env")
    print("   mlagents_env\\Scripts\\activate")
    print("4. Ejecuta este script nuevamente")
    print("=" * 50)

def main():
    """Función principal"""
    print("🔧 Arreglando instalación de ML-Agents")
    print("=" * 50)
    
    # Verificar versión de Python
    python_type = check_python_version()
    
    if python_type == "incompatible":
        print("\n❌ Versión de Python no compatible")
        suggest_python_installation()
        return
    
    if python_type == "python313":
        print("\n⚠️  Python 3.13 detectado. Intentando instalar versión compatible...")
        response = input("¿Continuar con Python 3.13? (s/n): ")
        if response.lower() != 's':
            suggest_python_installation()
            return
    
    # Desinstalar paquetes problemáticos
    uninstall_packages()
    
    # Instalar versiones correctas
    if not install_correct_versions(python_type):
        print("\n❌ Error durante la instalación")
        if python_type == "python313":
            print("\n💡 El problema puede ser Python 3.13")
            suggest_python_installation()
        return
    
    # Probar instalación
    if not test_installation():
        print("\n❌ La instalación no funciona correctamente")
        if python_type == "python313":
            print("\n💡 El problema es Python 3.13")
            suggest_python_installation()
        else:
            print("💡 Intenta:")
            print("   1. Usar Python 3.10-3.11")
            print("   2. Crear un entorno virtual")
            print("   3. Reinstalar desde cero")
        return
    
    # Crear script de entrenamiento
    create_training_script()
    
    print("\n🎉 ¡Instalación completada exitosamente!")
    print("\n📋 Próximos pasos:")
    print("   1. Abre Unity y tu proyecto")
    print("   2. Ve a ML Navigation → Crear Escenas 2D y 3D")
    print("   3. Abre Navigation3D y configura la capa Obstacle")
    print("   4. Ejecuta: python train_navigation.py")
    print("   5. Pulsa Play en Unity para iniciar entrenamiento")

if __name__ == "__main__":
    main()
