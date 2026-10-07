/**
 * Procedural material generator for gems and obstacles.
 * Creates PlayCanvas materials purely from color definitions — no texture files needed.
 */

export interface MaterialSpec {
  diffuse: string;
  emissive: string;
  specular: string;
  opacity: number;
  metallic: number;
  roughness: number;
}

export class MaterialFactory {
  /**
   * Generate a gem material spec from a hex color.
   * Gems use a translucent, glowing material.
   */
  static gemMaterial(hexColor: string): MaterialSpec {
    return {
      diffuse: this.dim(hexColor, 0.5),
      emissive: hexColor,
      specular: '#FFFFFF',
      opacity: 0.85,
      metallic: 0.3,
      roughness: 0.2,
    };
  }

  /**
   * Generate a special piece material.
   */
  static specialMaterial(type: 'line' | 'bomb' | 'colorbomb' | 'rocket'): MaterialSpec {
    const specs: Record<string, MaterialSpec> = {
      line: {
        diffuse: '#FFFFFF',
        emissive: '#FFFFFF',
        specular: '#FFFFFF',
        opacity: 0.9,
        metallic: 0.5,
        roughness: 0.1,
      },
      bomb: {
        diffuse: '#4A4A4A',
        emissive: '#FF8800',
        specular: '#FFFFFF',
        opacity: 0.95,
        metallic: 0.8,
        roughness: 0.3,
      },
      colorbomb: {
        diffuse: '#FFFFFF',
        emissive: '#FF69B4',
        specular: '#FFFFFF',
        opacity: 0.9,
        metallic: 0.6,
        roughness: 0.15,
      },
      rocket: {
        diffuse: '#C0C0C0',
        emissive: '#00CED1',
        specular: '#FFFFFF',
        opacity: 0.95,
        metallic: 0.9,
        roughness: 0.05,
      },
    };
    return specs[type] ?? specs.line;
  }

  /**
   * Generate an obstacle material.
   */
  static obstacleMaterial(content: string): MaterialSpec {
    const specs: Record<string, MaterialSpec> = {
      OBSTACLE_BOX: {
        diffuse: '#8B4513',
        emissive: '#8B4513',
        specular: '#000000',
        opacity: 1.0,
        metallic: 0.1,
        roughness: 0.8,
      },
      OBSTACLE_CAGE: {
        diffuse: '#8B0000',
        emissive: '#8B0000',
        specular: '#444444',
        opacity: 0.8,
        metallic: 0.4,
        roughness: 0.5,
      },
      OBSTACLE_ICE: {
        diffuse: '#87CEEB',
        emissive: '#87CEEB',
        specular: '#FFFFFF',
        opacity: 0.6,
        metallic: 0.0,
        roughness: 0.05,
      },
      OBSTACLE_STONE: {
        diffuse: '#808080',
        emissive: '#808080',
        specular: '#444444',
        opacity: 1.0,
        metallic: 0.2,
        roughness: 0.9,
      },
      BLOCKED: {
        diffuse: '#222222',
        emissive: '#222222',
        specular: '#000000',
        opacity: 1.0,
        metallic: 0.0,
        roughness: 1.0,
      },
    };
    return specs[content] ?? specs.OBSTACLE_BOX;
  }

  private static dim(hex: string, factor: number): string {
    const num = parseInt(hex.replace('#', ''), 16);
    const r = Math.round(((num >> 16) & 0xff) * factor);
    const g = Math.round(((num >> 8) & 0xff) * factor);
    const b = Math.round((num & 0xff) * factor);
    return `rgb(${r}, ${g}, ${b})`;
  }
}

export default MaterialFactory;
